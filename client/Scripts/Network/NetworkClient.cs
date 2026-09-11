using Godot;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.Network;

/// <summary>
/// PRD 39, 41 - ENet/UDP üzərindən dedicated serverə qoşulma.
/// PRD 46, 156 - Client YALNIZ input göndərir; health, pul, kill, rank
/// kimi heç bir gameplay nəticəsini özü təyin etmir.
/// </summary>
public sealed partial class NetworkClient : Node
{
    private ENetMultiplayerPeer? _peer;
    private uint _inputSequence;

    [Signal]
    public delegate void ConnectionSucceededEventHandler();

    [Signal]
    public delegate void ConnectionFailedEventHandler(string reason);

    public bool IsConnectedToServer => _peer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;

    public Error ConnectToServer(string address, int port = GameConstants.DefaultServerPort)
    {
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateClient(address, port);

        if (error != Error.Ok)
        {
            GD.PushError($"[NetworkClient] Qoşulma alınmadı: {error}");
            EmitSignal(SignalName.ConnectionFailed, error.ToString());
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;

        GD.Print($"[NetworkClient] {address}:{port} — {VersionGate.Describe()}");
        return Error.Ok;
    }

    /// <summary>PRD 43 - Client prediction üçün ardıcıl nömrələnmiş input.</summary>
    public InputCommand BuildInput(float moveForward, float moveRight, float yaw, float pitch, InputButtons buttons)
        => new(
            ++_inputSequence,
            Time.GetTicksMsec(),
            moveForward,
            moveRight,
            yaw,
            pitch,
            buttons);

    public void DisconnectFromServer()
    {
        _peer?.Close();
        _peer = null;
        Multiplayer.MultiplayerPeer = null;
    }

    private void OnConnectedToServer() => EmitSignal(SignalName.ConnectionSucceeded);

    private void OnConnectionFailed() => EmitSignal(SignalName.ConnectionFailed, "connection_failed");
}
