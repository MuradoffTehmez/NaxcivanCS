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
