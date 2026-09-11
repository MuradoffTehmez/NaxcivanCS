// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Buffers.Binary;
using System.Numerics;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Net;

/// <summary>
/// PRD 41, 44 - Snapshot-ların kompakt binar formatı.
///
/// UDP paketi kiçik olmalıdır, ona görə JSON istifadə edilmir. Format sabitdir və
/// <see cref="Constants.GameConstants.ProtocolVersion"/> ilə bağlıdır — dəyişdikdə
/// protokol versiyası artırılmalıdır (PRD 102).
///
/// <para>Layout: [tick u32][serverTime f64][count u16] sonra hər oyunçu üçün
/// [peerId i32][pos 3xf32][yaw f32][pitch f32][health u8][armor u8][flags u8][team u8][ackSeq u32].</para>
/// </summary>
public static class SnapshotSerializer
{
    /// <summary>Bir oyunçu qeydinin bayt ölçüsü.</summary>
    public const int PlayerStride = 4 + 12 + 4 + 4 + 1 + 1 + 1 + 1 + 4;

    /// <summary>Başlığın bayt ölçüsü.</summary>
    public const int HeaderSize = 4 + 8 + 2;

    private const byte FlagCrouching = 1 << 0;

    public static byte[] Serialize(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int count = snapshot.Players.Count;
        var buffer = new byte[HeaderSize + (count * PlayerStride)];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteUInt32LittleEndian(span[..4], snapshot.Tick);
        BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(4, 8), snapshot.ServerTimeMs);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(12, 2), (ushort)count);

        int offset = HeaderSize;
        foreach (PlayerSnapshot player in snapshot.Players)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), player.PeerId);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset + 4, 4), player.Position.X);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset + 8, 4), player.Position.Y);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset + 12, 4), player.Position.Z);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset + 16, 4), player.Yaw);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset + 20, 4), player.Pitch);

            span[offset + 24] = (byte)Math.Clamp(player.Health, 0, 255);
            span[offset + 25] = (byte)Math.Clamp(player.Armor, 0, 255);
            span[offset + 26] = player.IsCrouching ? FlagCrouching : (byte)0;
            span[offset + 27] = (byte)player.Team;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 28, 4), player.LastProcessedSequence);

            offset += PlayerStride;
        }

        return buffer;
    }

    public static WorldSnapshot Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
        {
            throw new ArgumentException("Snapshot başlığı natamamdır.", nameof(data));
        }

        uint tick = BinaryPrimitives.ReadUInt32LittleEndian(data[..4]);
        double serverTime = BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(4, 8));
        int count = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12, 2));

        int expected = HeaderSize + (count * PlayerStride);
        if (data.Length < expected)
        {
            throw new ArgumentException(
                $"Snapshot natamamdır: {data.Length} bayt, gözlənilən {expected}.", nameof(data));
        }

        var players = new List<PlayerSnapshot>(count);
        int offset = HeaderSize;

        for (int i = 0; i < count; i++)
        {
            int peerId = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
            var position = new Vector3(
                BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset + 4, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset + 8, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset + 12, 4)));

            players.Add(new PlayerSnapshot(
                peerId,
                position,
                BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset + 16, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset + 20, 4)),
                data[offset + 24],
                data[offset + 25],
                (data[offset + 26] & FlagCrouching) != 0,
                (Team)data[offset + 27],
                BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 28, 4))));

            offset += PlayerStride;
        }

        return new WorldSnapshot(tick, serverTime, players);
    }
}
