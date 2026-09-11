// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 41, 44, 46, 102 - Wire protokolunun round-trip testleri.</summary>
public sealed class NetworkProtocolTests
{
    [Fact]
    public void Snapshot_RoundTripsAllFields()
    {
        var original = new WorldSnapshot(
            Tick: 4242,
            ServerTimeMs: 98765.25,
            Players: new[]
            {
                new PlayerSnapshot(7, new Vector3(1.5f, 0.25f, -3.75f), 90f, -12f, 83, 41, true, Team.Alpha, 1234u),
                new PlayerSnapshot(9, new Vector3(-8f, 0f, 12f), 271f, 5f, 100, 0, false, Team.Bravo, 77u),
            });

        WorldSnapshot decoded = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(original));

        Assert.Equal(original.Tick, decoded.Tick);
        Assert.Equal(original.ServerTimeMs, decoded.ServerTimeMs);
        Assert.Equal(2, decoded.Players.Count);

        for (int i = 0; i < original.Players.Count; i++)
        {
            Assert.Equal(original.Players[i], decoded.Players[i]);
        }
    }

    [Fact]
    public void Snapshot_HasPredictableSize()
    {
        var snapshot = new WorldSnapshot(1, 0, new[]
        {
            new PlayerSnapshot(1, Vector3.Zero, 0f, 0f, 100, 0, false, Team.Alpha, 0u),
        });

        byte[] bytes = SnapshotSerializer.Serialize(snapshot);
        Assert.Equal(SnapshotSerializer.HeaderSize + SnapshotSerializer.PlayerStride, bytes.Length);
    }

    [Fact]
    public void Snapshot_FullTenPlayerPacketStaysSmall()
    {
        var players = new List<PlayerSnapshot>();
        for (int i = 0; i < GameConstants.MaxPlayers; i++)
        {
            players.Add(new PlayerSnapshot(i, Vector3.One, 0f, 0f, 100, 100, false, Team.Alpha, 0u));
        }

        byte[] bytes = SnapshotSerializer.Serialize(new WorldSnapshot(1, 0, players));

        // 10 oyunculu tam snapshot tipik MTU-dan (1200 bayt) xeyli kicik olmalidir.
        Assert.True(bytes.Length < 1200, $"Snapshot {bytes.Length} bayt — cox boyukdur");
    }

    [Fact]
    public void Snapshot_TruncatedPayloadIsRejected()
    {
        var snapshot = new WorldSnapshot(1, 0, new[]
        {
            new PlayerSnapshot(1, Vector3.Zero, 0f, 0f, 100, 0, false, Team.Alpha, 0u),
        });

        byte[] bytes = SnapshotSerializer.Serialize(snapshot);
        Assert.Throws<ArgumentException>(() => SnapshotSerializer.Deserialize(bytes.AsSpan(0, bytes.Length - 4)));
    }

    [Fact]
    public void Input_RoundTripsThroughPacketCodec()
    {
        var input = new InputCommand(
            Sequence: 909,
            ClientTimeMs: 12345.5,
            MoveForward: 1f,
            MoveRight: -1f,
            YawDegrees: 128.25f,
            PitchDegrees: -33.5f,
            Buttons: InputButtons.Jump | InputButtons.PrimaryFire);

        byte[] packet = PacketCodec.EncodeInput(input);

        Assert.Equal(MessageType.InputCommand, PacketCodec.ReadType(packet));
        Assert.Equal(input, PacketCodec.DecodeInput(PacketCodec.Payload(packet)));
    }

    [Theory]
    [InlineData(5f, 1f)]
    [InlineData(-5f, -1f)]
    [InlineData(0.5f, 0.5f)]
    public void Input_MovementAxesAreClamped(float sent, float expected)
    {
        // PRD 47 - Deyisdirilmis client boyuk ox deyeri gonderib suret qazana bilmemelidir.
        var input = new InputCommand(1, 0, sent, sent, 0f, 0f, InputButtons.None);
        InputCommand decoded = PacketCodec.DecodeInput(PacketCodec.Payload(PacketCodec.EncodeInput(input)));

        Assert.Equal(expected, decoded.MoveForward);
        Assert.Equal(expected, decoded.MoveRight);
    }

    [Fact]
    public void Input_NaNAxisBecomesZero()
    {
        var input = new InputCommand(1, 0, float.NaN, float.NaN, 0f, 0f, InputButtons.None);
        InputCommand decoded = PacketCodec.DecodeInput(PacketCodec.Payload(PacketCodec.EncodeInput(input)));

        Assert.Equal(0f, decoded.MoveForward);
        Assert.Equal(0f, decoded.MoveRight);
    }

    [Fact]
    public void Handshake_RoundTrips()
    {
        byte[] packet = PacketCodec.EncodeHandshake(GameConstants.ProtocolVersion, "Tahmaz");

        Assert.Equal(MessageType.Handshake, PacketCodec.ReadType(packet));

        (int version, string username) = PacketCodec.DecodeHandshake(PacketCodec.Payload(packet));
        Assert.Equal(GameConstants.ProtocolVersion, version);
        Assert.Equal("Tahmaz", username);
    }

    [Fact]
    public void Handshake_SupportsNonAsciiUsernames()
    {
        byte[] packet = PacketCodec.EncodeHandshake(1, "Şəhriyar");
        (_, string username) = PacketCodec.DecodeHandshake(PacketCodec.Payload(packet));

        Assert.Equal("Şəhriyar", username);
    }

    [Fact]
    public void HandshakeAccepted_RoundTrips()
    {
        byte[] packet = PacketCodec.EncodeHandshakeAccepted(42, Team.Bravo, 1234.5);
        (int peerId, Team team, double serverTime) = PacketCodec.DecodeHandshakeAccepted(PacketCodec.Payload(packet));

        Assert.Equal(42, peerId);
        Assert.Equal(Team.Bravo, team);
        Assert.Equal(1234.5, serverTime);
    }

    [Fact]
    public void Damage_UsesKilledMessageTypeWhenLethal()
    {
        byte[] hurt = PacketCodec.EncodeDamage(2, 1, HitBox.Chest, 34, killed: false);
        byte[] killed = PacketCodec.EncodeDamage(2, 1, HitBox.Head, 136, killed: true);

        Assert.Equal(MessageType.PlayerDamaged, PacketCodec.ReadType(hurt));
        Assert.Equal(MessageType.PlayerKilled, PacketCodec.ReadType(killed));

        (int victim, int attacker, HitBox hitBox, int damage, bool wasKilled) =
            PacketCodec.DecodeDamage(PacketCodec.Payload(killed));

        Assert.Equal(2, victim);
        Assert.Equal(1, attacker);
        Assert.Equal(HitBox.Head, hitBox);
        Assert.Equal(136, damage);
        Assert.True(wasKilled);
    }

    [Fact]
    public void VersionGate_RejectsMismatchedProtocol()
    {
        Assert.True(VersionGate.IsCompatible(GameConstants.ProtocolVersion));
        Assert.False(VersionGate.IsCompatible(GameConstants.ProtocolVersion + 1));
        Assert.False(VersionGate.IsCompatible(1)); // Pre-gunplay clients lack shot/ammo messages.
    }
}
