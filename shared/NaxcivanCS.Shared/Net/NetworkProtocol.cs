using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Shared.Net;

/// <summary>
/// PRD 41, 46, 102 - ENet/UDP üzərində mesaj növləri.
/// PRD 46: client YALNIZ input göndərir, "hit etdim" demir.
/// </summary>
public enum MessageType : ushort
{
    // Client -> Server
    Handshake = 1,
    InputCommand = 2,
    BuyRequest = 3,
    ChatMessage = 4,
    TeamSelect = 5,
    Disconnect = 6,

    // Server -> Client
    HandshakeAccepted = 100,
    HandshakeRejected = 101,
    WorldSnapshot = 102,
    RoundStateChanged = 103,
    PlayerDamaged = 104,
    PlayerKilled = 105,
    BombPlanted = 106,
    BombDefused = 107,
    MatchEnded = 108,
    BuyResult = 109,
}

/// <summary>PRD 43, 46 - Client-dən gələn yeganə gameplay girişi.</summary>
public readonly record struct InputCommand(
    uint Sequence,
    double ClientTimeMs,
    float MoveForward,
    float MoveRight,
    float YawDegrees,
    float PitchDegrees,
    InputButtons Buttons);

/// <summary>PRD 11 - Default controls-a uyğun bit maskası.</summary>
[Flags]
public enum InputButtons : uint
{
    None = 0,
    Jump = 1 << 0,
    Crouch = 1 << 1,
    Walk = 1 << 2,
    PrimaryFire = 1 << 3,
    SecondaryFire = 1 << 4,
    Reload = 1 << 5,
    Interact = 1 << 6,
    Drop = 1 << 7,
    BuyMenu = 1 << 8,
    Scoreboard = 1 << 9,
}

/// <summary>PRD 102 - Uyğun olmayan client serverə qoşulmamalıdır.</summary>
public static class VersionGate
{
    public static bool IsCompatible(int clientProtocolVersion)
        => clientProtocolVersion == GameConstants.ProtocolVersion;

    public static string Describe()
        => $"protocol={GameConstants.ProtocolVersion}; game={GameConstants.GameVersion}; content={GameConstants.ContentVersion}";
}
