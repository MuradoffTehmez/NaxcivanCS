using Godot;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Server.Network;

/// <summary>
/// PRD 39, 40, 41 - Server Authoritative Dedicated Server.
/// PRD 102 - Uyğun olmayan protokol versiyalı client qəbul edilmir.
/// </summary>
public sealed partial class NetworkServer : Node
{
    private ENetMultiplayerPeer? _peer;

    public int ConnectedPlayers { get; private set; }

    public Error Listen(int port = GameConstants.DefaultServerPort, int maxPlayers = GameConstants.MaxPlayers)
    {
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateServer(port, maxPlayers);

        if (error != Error.Ok)
        {
            GD.PushError($"[NetworkServer] Port {port} dinlənilə bilmədi: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        GD.Print($"[NetworkServer] Port {port}, tick={GameConstants.ServerTickRate}, {VersionGate.Describe()}");
        return Error.Ok;
    }

    /// <summary>PRD 102 - Handshake versiya yoxlaması.</summary>
    public bool AcceptHandshake(long peerId, int clientProtocolVersion)
    {
        if (VersionGate.IsCompatible(clientProtocolVersion))
        {
            return true;
        }

        GD.Print($"[NetworkServer] Peer {peerId} rədd edildi — protocol {clientProtocolVersion}");
        _peer?.DisconnectPeer((int)peerId);
        return false;
    }

    private void OnPeerConnected(long peerId)
    {
        ConnectedPlayers++;
        GD.Print($"[NetworkServer] Peer qoşuldu: {peerId} ({ConnectedPlayers}/{GameConstants.MaxPlayers})");
    }

    private void OnPeerDisconnected(long peerId)
    {
        ConnectedPlayers = Math.Max(0, ConnectedPlayers - 1);
        GD.Print($"[NetworkServer] Peer ayrıldı: {peerId} ({ConnectedPlayers}/{GameConstants.MaxPlayers})");
    }
}
