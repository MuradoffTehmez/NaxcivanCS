// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

public sealed class RoundProtocolTests
{
    [Fact]
    public void RoundState_RoundTripsAllFields()
    {
        byte[] packet = PacketCodec.EncodeRoundState(RoundPhase.RoundEnd, MatchState.Overtime,
            3.5f, 25, 12, 13, Team.Bravo, RoundEndReason.BombDefused);
        Assert.Equal(MessageType.RoundStateChanged, PacketCodec.ReadType(packet));
        var state = PacketCodec.DecodeRoundState(PacketCodec.Payload(packet));
        Assert.Equal((RoundPhase.RoundEnd, MatchState.Overtime, 3.5f, 25, 12, 13,
            Team.Bravo, RoundEndReason.BombDefused), state);
    }

    [Fact]
    public void RoundState_ClampsValuesToWireLimits()
    {
        byte[] packet = PacketCodec.EncodeRoundState(RoundPhase.Active, MatchState.Overtime,
            -1, int.MaxValue, -1, 300, Team.None, RoundEndReason.TimeExpired);
        var state = PacketCodec.DecodeRoundState(PacketCodec.Payload(packet));
        Assert.Equal(0f, state.TimeRemaining);
        Assert.Equal(ushort.MaxValue, state.RoundNumber);
        Assert.Equal(0, state.AlphaScore);
        Assert.Equal(255, state.BravoScore);
    }

    [Fact]
    public void Scoreboard_RoundTripsUtf8AndPlayerState()
    {
        PacketCodec.ScoreboardEntry[] entries =
        [
            new(7, "Əli", Team.Alpha, 4, 2, 3200, true),
            new(8, "Ömər", Team.Bravo, 2, 4, 800, false),
        ];
        byte[] packet = PacketCodec.EncodeScoreboard(entries);
        Assert.Equal(MessageType.Scoreboard, PacketCodec.ReadType(packet));
        Assert.Equal(entries, PacketCodec.DecodeScoreboard(PacketCodec.Payload(packet)));
    }

    [Fact]
    public void Scoreboard_TruncatedPayloadsAreRejected()
    {
        byte[] packet = PacketCodec.EncodeScoreboard([new(1, "Player", Team.Alpha, 0, 0, 800, true)]);
        byte[] payload = PacketCodec.Payload(packet).ToArray();
        for (int length = 0; length < payload.Length; length++)
        {
            byte[] truncated = payload[..length];
            Assert.Throws<ArgumentException>(() => PacketCodec.DecodeScoreboard(truncated));
        }
    }

    [Fact]
    public void RoundState_TruncatedPayloadsAreRejected()
    {
        for (int length = 0; length < 12; length++)
        {
            byte[] truncated = new byte[length];
            Assert.Throws<ArgumentException>(() => PacketCodec.DecodeRoundState(truncated));
        }
    }

    [Fact]
    public void Scoreboard_EmptyRosterIsValid()
    {
        byte[] packet = PacketCodec.EncodeScoreboard([]);
        Assert.Empty(PacketCodec.DecodeScoreboard(PacketCodec.Payload(packet)));
    }
}
