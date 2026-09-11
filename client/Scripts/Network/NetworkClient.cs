using Godot;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.Network;

/// <summary>
/// PRD 39, 41 - ENet/UDP üzərindən dedicated serverə qoşulma.
///
/// PRD 46, 156 - Client YALNIZ input göndərir. Health, pul, kill, rank kimi
/// heç bir gameplay nəticəsini özü təyin etmir; hamısını serverdən alır.
/// </summary>
public sealed partial class NetworkClient : Node
{
    private ENetMultiplayerPeer? _peer;
    private uint _inputSequence;
    private bool _connectionAnnounced;

    [Signal]
    public delegate void HandshakeAcceptedEventHandler(int peerId, int team);

    [Signal]
    public delegate void HandshakeRejectedEventHandler(string reason);

    [Signal]
    public delegate void SnapshotReceivedEventHandler(byte[] payload);

    [Signal]
    public delegate void DamageReceivedEventHandler(int victimPeerId, int attackerPeerId, int hitBox, int healthDamage, bool killed);

    [Signal]
    public delegate void DisconnectedEventHandler();

    /// <summary>Serverin bizə verdiyi peer id — handshake-dən sonra dolur.</summary>
    public int LocalPeerId { get; private set; }

    public Team LocalTeam { get; private set; } = Team.None;

    public bool IsConnectedToServer =>
        _peer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;

    /// <summary>Son ölçülən gediş-gəliş vaxtı, ms (PRD 152 - ping göstəricisi).</summary>
    public double PingMs { get; private set; }

    public string Username { get; set; } = "Player";

    public Error ConnectToServer(string address, int port = GameConstants.DefaultServerPort)
    {
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateClient(address, port);

        if (error != Error.Ok)
        {
            GD.PushError($"[NetworkClient] Qoşulma alınmadı: {error}");
            _peer = null;
            return error;
        }

        GD.Print($"[NetworkClient] {address}:{port} — {VersionGate.Describe()}");
        return Error.Ok;
    }

    public override void _Process(double delta)
    {
        if (_peer is null)
        {
            return;
        }

        _peer.Poll();

        MultiplayerPeer.ConnectionStatus status = _peer.GetConnectionStatus();

        if (status == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            if (_connectionAnnounced)
            {
                _connectionAnnounced = false;
                EmitSignal(SignalName.Disconnected);
            }

            return;
        }

        if (status == MultiplayerPeer.ConnectionStatus.Connected && !_connectionAnnounced)
        {
            _connectionAnnounced = true;
            SendHandshake();
        }

        UpdatePing();

        while (_peer.GetAvailablePacketCount() > 0)
        {
            byte[] packet = _peer.GetPacket();

            try
            {
                HandlePacket(packet);
            }
            catch (ArgumentException ex)
            {
                GD.PushWarning($"[NetworkClient] Yanlış paket: {ex.Message}");
            }
        }
    }

    /// <summary>PRD 43, 46 - Ardıcıl nömrələnmiş input yaradır (prediction üçün lazımdır).</summary>
    public InputCommand BuildInput(float moveForward, float moveRight, float yaw, float pitch, InputButtons buttons)
        => new(
            ++_inputSequence,
            Time.GetTicksMsec(),
            moveForward,
            moveRight,
            yaw,
            pitch,
            buttons);

    public void SendInput(InputCommand input)
    {
        // Input-lar unreliable gedir: köhnəlmiş input-u yenidən göndərmək mənasızdır,
        // növbəti paket onsuz da daha yeni vəziyyəti daşıyır (PRD 43).
        Send(PacketCodec.EncodeInput(input), reliable: false);
    }

    public void DisconnectFromServer()
    {
        if (_peer is null)
        {
            return;
        }

        Send(PacketCodec.Wrap(MessageType.Disconnect, ReadOnlySpan<byte>.Empty));
        _peer.Close();
        _peer = null;
        _connectionAnnounced = false;
    }

    private void SendHandshake()
        => Send(PacketCodec.EncodeHandshake(GameConstants.ProtocolVersion, Username));

    private void HandlePacket(byte[] packet)
    {
        MessageType type = PacketCodec.ReadType(packet);
        ReadOnlySpan<byte> payload = PacketCodec.Payload(packet);

        switch (type)
        {
            case MessageType.HandshakeAccepted:
            {
                (int peerId, Team team, double _) = PacketCodec.DecodeHandshakeAccepted(payload);
                LocalPeerId = peerId;
                LocalTeam = team;
                GD.Print($"[NetworkClient] Qəbul edildi: peer={peerId}, team={team}");
                EmitSignal(SignalName.HandshakeAccepted, peerId, (int)team);
                break;
            }

            case MessageType.HandshakeRejected:
            {
                string reason = PacketCodec.DecodeHandshakeRejected(payload);
                GD.PushWarning($"[NetworkClient] Server rədd etdi: {reason}");
                EmitSignal(SignalName.HandshakeRejected, reason);
                break;
            }

            case MessageType.WorldSnapshot:
                EmitSignal(SignalName.SnapshotReceived, payload.ToArray());
                break;

            case MessageType.PlayerDamaged:
            case MessageType.PlayerKilled:
            {
                (int victim, int attacker, HitBox hitBox, int damage, bool killed) = PacketCodec.DecodeDamage(payload);
                EmitSignal(SignalName.DamageReceived, victim, attacker, (int)hitBox, damage, killed);
                break;
            }

            default:
                break;
        }
    }

    private void UpdatePing()
    {
        if (_peer?.GetPeer(1) is { } serverPeer)
        {
            PingMs = serverPeer.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
        }
    }

    private void Send(byte[] packet, bool reliable = true)
    {
        if (_peer is null)
        {
            return;
        }

        _peer.SetTargetPeer(1); // 1 = server
        _peer.TransferMode = reliable
            ? MultiplayerPeer.TransferModeEnum.Reliable
            : MultiplayerPeer.TransferModeEnum.Unreliable;

        _peer.PutPacket(packet);
    }

    public override void _ExitTree() => DisconnectFromServer();
}
