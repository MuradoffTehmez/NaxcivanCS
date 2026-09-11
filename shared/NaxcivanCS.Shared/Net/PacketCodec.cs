// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Buffers.Binary;
using System.Text;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Net;

/// <summary>
/// PRD 41, 46, 102 - NaxcivanCS wire protokolu.
///
/// Hər paket 2 baytlıq <see cref="MessageType"/> başlığı ilə başlayır, ardınca
/// növə xas faydalı yük gəlir. Godot-un yüksək səviyyəli RPC-si istifadə edilmir:
/// client və server ayrı layihələrdir və paket ölçüsü üzərində tam nəzarət lazımdır.
///
/// <para>Format dəyişdikdə <see cref="GameConstants.ProtocolVersion"/> artırılmalıdır.</para>
/// </summary>
public static class PacketCodec
{
    public const int HeaderSize = 2;

    private const int MaxUsernameBytes = 64;

    public static MessageType ReadType(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < HeaderSize)
        {
            throw new ArgumentException("Paket başlığı natamamdır.", nameof(packet));
        }

        return (MessageType)BinaryPrimitives.ReadUInt16LittleEndian(packet[..HeaderSize]);
    }

    public static ReadOnlySpan<byte> Payload(ReadOnlySpan<byte> packet) => packet[HeaderSize..];

    /// <summary>
    /// Hazır faydalı yükü başlıqla bükür. Artıq serializasiya olunmuş yükü
    /// yenidən emal etməmək üçün açıqdır (məs. snapshot yayımı).
    /// </summary>
    public static byte[] Wrap(MessageType type, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, HeaderSize), (ushort)type);
        payload.CopyTo(packet.AsSpan(HeaderSize));
        return packet;
    }

    // ---------------------------------------------------------------- Handshake

    /// <summary>PRD 102 - [protocolVersion i32][usernameLength u8][username utf8].</summary>
    public static byte[] EncodeHandshake(int protocolVersion, string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        byte[] nameBytes = Encoding.UTF8.GetBytes(username);
        if (nameBytes.Length > MaxUsernameBytes)
        {
            throw new ArgumentException($"Username {MaxUsernameBytes} baytdan uzundur.", nameof(username));
        }

        var payload = new byte[4 + 1 + nameBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), protocolVersion);
        payload[4] = (byte)nameBytes.Length;
        nameBytes.CopyTo(payload.AsSpan(5));

        return Wrap(MessageType.Handshake, payload);
    }

    public static (int ProtocolVersion, string Username) DecodeHandshake(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
        {
            throw new ArgumentException("Handshake faydalı yükü natamamdır.", nameof(payload));
        }

        int protocolVersion = BinaryPrimitives.ReadInt32LittleEndian(payload[..4]);
        int nameLength = payload[4];

        if (payload.Length < 5 + nameLength)
        {
            throw new ArgumentException("Handshake username sahəsi natamamdır.", nameof(payload));
        }

        return (protocolVersion, Encoding.UTF8.GetString(payload.Slice(5, nameLength)));
    }

    /// <summary>[peerId i32][team u8][serverTimeMs f64] — client öz kimliyini öyrənir.</summary>
    public static byte[] EncodeHandshakeAccepted(int peerId, Team team, double serverTimeMs)
    {
        var payload = new byte[4 + 1 + 8];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), peerId);
        payload[4] = (byte)team;
        BinaryPrimitives.WriteDoubleLittleEndian(payload.AsSpan(5, 8), serverTimeMs);
        return Wrap(MessageType.HandshakeAccepted, payload);
    }

    public static (int PeerId, Team Team, double ServerTimeMs) DecodeHandshakeAccepted(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 13)
        {
            throw new ArgumentException("HandshakeAccepted faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            BinaryPrimitives.ReadInt32LittleEndian(payload[..4]),
            (Team)payload[4],
            BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(5, 8)));
    }

    public static byte[] EncodeHandshakeRejected(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return Wrap(MessageType.HandshakeRejected, Encoding.UTF8.GetBytes(reason));
    }

    public static string DecodeHandshakeRejected(ReadOnlySpan<byte> payload) => Encoding.UTF8.GetString(payload);

    // ------------------------------------------------------------ InputCommand

    /// <summary>
    /// PRD 46 - Client-dən serverə gedən YEGANƏ gameplay paketi.
    /// [sequence u32][clientTime f64][forward f32][right f32][yaw f32][pitch f32][buttons u32]
    /// </summary>
    public static byte[] EncodeInput(InputCommand input)
    {
        var payload = new byte[4 + 8 + 4 + 4 + 4 + 4 + 4];
        Span<byte> span = payload;

        BinaryPrimitives.WriteUInt32LittleEndian(span[..4], input.Sequence);
        BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(4, 8), input.ClientTimeMs);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(12, 4), input.MoveForward);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(16, 4), input.MoveRight);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(20, 4), input.YawDegrees);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(24, 4), input.PitchDegrees);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28, 4), (uint)input.Buttons);

        return Wrap(MessageType.InputCommand, payload);
    }

    public static InputCommand DecodeInput(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 32)
        {
            throw new ArgumentException("Input faydalı yükü natamamdır.", nameof(payload));
        }

        return new InputCommand(
            BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]),
            BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(4, 8)),
            ClampAxis(BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(12, 4))),
            ClampAxis(BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(16, 4))),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(20, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(24, 4)),
            (InputButtons)BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(28, 4)));
    }

    /// <summary>
    /// PRD 47 - Hərəkət oxları [-1, 1] aralığına klamp edilir ki, dəyişdirilmiş
    /// client böyük dəyər göndərərək sürət üstünlüyü qazana bilməsin.
    /// </summary>
    private static float ClampAxis(float value)
        => float.IsNaN(value) ? 0f : Math.Clamp(value, -1f, 1f);

    // ---------------------------------------------------------------- Snapshot

    public static byte[] EncodeSnapshot(WorldSnapshot snapshot)
        => Wrap(MessageType.WorldSnapshot, SnapshotSerializer.Serialize(snapshot));

    public static WorldSnapshot DecodeSnapshot(ReadOnlySpan<byte> payload)
        => SnapshotSerializer.Deserialize(payload);

    // ----------------------------------------------------------- Shot events

    /// <summary>
    /// PRD 17, 46 - Serverin hesabladığı atəş.
    /// Client bundan muzzle flash, tracer və kamera kick-i üçün istifadə edir;
    /// atəşin özünü və istiqamətini <b>server</b> təyin edir.
    ///
    /// [shooterId i32][origin 3xf32][end 3xf32][punchPitch f32][punchYaw f32]
    /// [shotIndex u16][hit u8]
    /// </summary>
    public static byte[] EncodeShotFired(
        int shooterPeerId,
        System.Numerics.Vector3 origin,
        System.Numerics.Vector3 end,
        float punchPitch,
        float punchYaw,
        int shotIndex,
        bool hit)
    {
        var payload = new byte[4 + 12 + 12 + 4 + 4 + 2 + 1];
        Span<byte> span = payload;

        BinaryPrimitives.WriteInt32LittleEndian(span[..4], shooterPeerId);
        WriteVector(span.Slice(4, 12), origin);
        WriteVector(span.Slice(16, 12), end);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(28, 4), punchPitch);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(32, 4), punchYaw);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(36, 2), (ushort)Math.Clamp(shotIndex, 0, ushort.MaxValue));
        span[38] = hit ? (byte)1 : (byte)0;

        return Wrap(MessageType.ShotFired, payload);
    }

    public static (int ShooterPeerId, System.Numerics.Vector3 Origin, System.Numerics.Vector3 End,
        float PunchPitch, float PunchYaw, int ShotIndex, bool Hit) DecodeShotFired(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 39)
        {
            throw new ArgumentException("ShotFired faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            BinaryPrimitives.ReadInt32LittleEndian(payload[..4]),
            ReadVector(payload.Slice(4, 12)),
            ReadVector(payload.Slice(16, 12)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(28, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(32, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(36, 2)),
            payload[38] != 0);
    }

    /// <summary>
    /// PRD 78 - Şarjor vəziyyəti (yalnız sahibinə göndərilir).
    /// [magazine u16][reserve u16][reloading u8]
    /// </summary>
    public static byte[] EncodeWeaponState(int magazine, int reserve, bool reloading)
    {
        var payload = new byte[2 + 2 + 1];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), (ushort)Math.Clamp(magazine, 0, ushort.MaxValue));
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), (ushort)Math.Clamp(reserve, 0, ushort.MaxValue));
        payload[4] = reloading ? (byte)1 : (byte)0;

        return Wrap(MessageType.WeaponState, payload);
    }

    public static (int Magazine, int Reserve, bool Reloading) DecodeWeaponState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
        {
            throw new ArgumentException("WeaponState faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            BinaryPrimitives.ReadUInt16LittleEndian(payload[..2]),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)),
            payload[4] != 0);
    }

    private static void WriteVector(Span<byte> span, System.Numerics.Vector3 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span[..4], value.X);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(4, 4), value.Y);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(8, 4), value.Z);
    }

    private static System.Numerics.Vector3 ReadVector(ReadOnlySpan<byte> span) => new(
        BinaryPrimitives.ReadSingleLittleEndian(span[..4]),
        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(4, 4)),
        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(8, 4)));

    // ------------------------------------------------------------ Round state

    /// <summary>
    /// PRD 10, 128 - Round və match vəziyyəti.
    /// [phase u8][matchState u8][timeRemaining f32][roundNumber u16]
    /// [alphaScore u8][bravoScore u8][roundWinner u8][endReason u8]
    /// </summary>
    public static byte[] EncodeRoundState(
        RoundPhase phase,
        MatchState matchState,
        float timeRemaining,
        int roundNumber,
        int alphaScore,
        int bravoScore,
        Team roundWinner,
        RoundEndReason endReason)
    {
        var payload = new byte[1 + 1 + 4 + 2 + 1 + 1 + 1 + 1];
        Span<byte> span = payload;

        span[0] = (byte)phase;
        span[1] = (byte)matchState;
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(2, 4), MathF.Max(0f, timeRemaining));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6, 2), (ushort)Math.Clamp(roundNumber, 0, ushort.MaxValue));
        span[8] = (byte)Math.Clamp(alphaScore, 0, 255);
        span[9] = (byte)Math.Clamp(bravoScore, 0, 255);
        span[10] = (byte)roundWinner;
        span[11] = (byte)endReason;

        return Wrap(MessageType.RoundStateChanged, payload);
    }

    public static (RoundPhase Phase, MatchState MatchState, float TimeRemaining, int RoundNumber,
        int AlphaScore, int BravoScore, Team RoundWinner, RoundEndReason EndReason)
        DecodeRoundState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            throw new ArgumentException("RoundState faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            (RoundPhase)payload[0],
            (MatchState)payload[1],
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(2, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2)),
            payload[8],
            payload[9],
            (Team)payload[10],
            (RoundEndReason)payload[11]);
    }

    // ------------------------------------------------------------------ Bomb

    /// <summary>PRD 8 - Site adını bir bayta yığır; 0 = yerləşdirilməyib.</summary>
    private static byte EncodeSite(string site) => site switch
    {
        "A" => 1,
        "B" => 2,
        _ => 0,
    };

    private static string DecodeSite(byte value) => value switch
    {
        1 => "A",
        2 => "B",
        _ => string.Empty,
    };

    /// <summary>
    /// PRD 8 - Bomba vəziyyəti.
    /// [state u8][carrierPeerId i32][x f32][y f32][z f32]
    /// [plantProgress f32][defuseProgress f32][site u8]
    /// </summary>
    public static byte[] EncodeBombState(
        BombState state,
        int carrierPeerId,
        System.Numerics.Vector3 position,
        float plantProgress,
        float defuseProgress,
        string plantedSite)
    {
        var payload = new byte[1 + 4 + 12 + 4 + 4 + 1];
        Span<byte> span = payload;

        span[0] = (byte)state;
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(1, 4), carrierPeerId);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(5, 4), position.X);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(9, 4), position.Y);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(13, 4), position.Z);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(17, 4), Math.Clamp(plantProgress, 0f, 1f));
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(21, 4), Math.Clamp(defuseProgress, 0f, 1f));
        span[25] = EncodeSite(plantedSite);

        return Wrap(MessageType.BombStateChanged, payload);
    }

    public static (BombState State, int CarrierPeerId, System.Numerics.Vector3 Position,
        float PlantProgress, float DefuseProgress, string PlantedSite)
        DecodeBombState(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 26)
        {
            throw new ArgumentException("BombState faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            (BombState)payload[0],
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(1, 4)),
            new System.Numerics.Vector3(
                BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(5, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(9, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(13, 4))),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(17, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(21, 4)),
            DecodeSite(payload[25]));
    }

    // ------------------------------------------------------------- Scoreboard

    /// <summary>Scoreboard sətrinin dəyişməz hissəsinin ölçüsü (ad istisna).</summary>
    private const int ScoreboardEntryHeaderSize = 4 + 1 + 2 + 2 + 2 + 1 + 1;

    /// <summary>PRD 69, 128 - Scoreboard sətri.</summary>
    public readonly record struct ScoreboardEntry(
        int PeerId, string Username, Team Team, int Kills, int Deaths, int Money, bool IsAlive);

    /// <summary>
    /// PRD 128 - Scoreboard. Nadir göndərilir (round sonu), ona görə
    /// username-lər hər dəfə daxil edilir — ayrıca ad reyestri lazım deyil.
    /// </summary>
    public static byte[] EncodeScoreboard(IReadOnlyList<ScoreboardEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var buffer = new List<byte>(64) { (byte)Math.Min(entries.Count, 255) };

        // CA2014 - stackalloc dövrədən kənarda: sətir sayı 255-ə qədər ola bilər.
        Span<byte> fixedPart = stackalloc byte[ScoreboardEntryHeaderSize];

        foreach (ScoreboardEntry entry in entries.Take(255))
        {
            byte[] name = Encoding.UTF8.GetBytes(entry.Username);
            if (name.Length > MaxUsernameBytes)
            {
                name = name[..MaxUsernameBytes];
            }

            BinaryPrimitives.WriteInt32LittleEndian(fixedPart[..4], entry.PeerId);
            fixedPart[4] = (byte)entry.Team;
            BinaryPrimitives.WriteUInt16LittleEndian(fixedPart.Slice(5, 2), (ushort)Math.Clamp(entry.Kills, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(fixedPart.Slice(7, 2), (ushort)Math.Clamp(entry.Deaths, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(fixedPart.Slice(9, 2), (ushort)Math.Clamp(entry.Money, 0, ushort.MaxValue));
            fixedPart[11] = entry.IsAlive ? (byte)1 : (byte)0;
            fixedPart[12] = (byte)name.Length;

            buffer.AddRange(fixedPart.ToArray());
            buffer.AddRange(name);
        }

        return Wrap(MessageType.Scoreboard, buffer.ToArray());
    }

    public static IReadOnlyList<ScoreboardEntry> DecodeScoreboard(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 1)
        {
            throw new ArgumentException("Scoreboard faydalı yükü boşdur.", nameof(payload));
        }

        int count = payload[0];
        var entries = new List<ScoreboardEntry>(count);
        int offset = 1;

        for (int i = 0; i < count; i++)
        {
            if (offset + ScoreboardEntryHeaderSize > payload.Length)
            {
                throw new ArgumentException("Scoreboard sətri natamamdır.", nameof(payload));
            }

            int peerId = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(offset, 4));
            var team = (Team)payload[offset + 4];
            int kills = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset + 5, 2));
            int deaths = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset + 7, 2));
            int money = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset + 9, 2));
            bool alive = payload[offset + 11] != 0;
            int nameLength = payload[offset + 12];

            offset += ScoreboardEntryHeaderSize;

            if (offset + nameLength > payload.Length)
            {
                throw new ArgumentException("Scoreboard username sahəsi natamamdır.", nameof(payload));
            }

            string username = Encoding.UTF8.GetString(payload.Slice(offset, nameLength));
            offset += nameLength;

            entries.Add(new ScoreboardEntry(peerId, username, team, kills, deaths, money, alive));
        }

        return entries;
    }

    // ------------------------------------------------------------ Damage events

    /// <summary>[victimPeerId i32][attackerPeerId i32][hitBox u8][healthDamage u16][killed u8]</summary>
    public static byte[] EncodeDamage(int victimPeerId, int attackerPeerId, HitBox hitBox, int healthDamage, bool killed)
    {
        var payload = new byte[4 + 4 + 1 + 2 + 1];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), victimPeerId);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), attackerPeerId);
        payload[8] = (byte)hitBox;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(9, 2), (ushort)Math.Clamp(healthDamage, 0, ushort.MaxValue));
        payload[11] = killed ? (byte)1 : (byte)0;

        return Wrap(killed ? MessageType.PlayerKilled : MessageType.PlayerDamaged, payload);
    }

    public static (int VictimPeerId, int AttackerPeerId, HitBox HitBox, int HealthDamage, bool Killed)
        DecodeDamage(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            throw new ArgumentException("Damage faydalı yükü natamamdır.", nameof(payload));
        }

        return (
            BinaryPrimitives.ReadInt32LittleEndian(payload[..4]),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            (HitBox)payload[8],
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(9, 2)),
            payload[11] != 0);
    }
}
