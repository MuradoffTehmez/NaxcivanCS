using Godot;
using NaxcivanCS.Server.Damage;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Server.ServerCore;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Server.Network;

/// <summary>
/// PRD 39, 40, 41 - Server Authoritative Dedicated Server şəbəkə qatı.
///
/// Godot-un yüksək səviyyəli RPC-si istifadə edilmir — xam ENet paketləri
/// <see cref="PacketCodec"/> ilə oxunub yazılır. Bu, client və serverin ayrı
/// layihələr olmasına və paket ölçüsü üzərində tam nəzarətə imkan verir.
/// </summary>
public sealed partial class NetworkServer : Node
{
    private readonly Dictionary<int, bool> _handshaked = new();
    private ENetMultiplayerPeer? _peer;
    private GameWorld? _world;

    public int ConnectedPlayers => _handshaked.Count;

    public void Attach(GameWorld world)
    {
        _world = world;
        world.SnapshotReady += OnSnapshotReady;
    }

    public Error Listen(int port = GameConstants.DefaultServerPort, int maxPlayers = GameConstants.MaxPlayers)
    {
        _peer = new ENetMultiplayerPeer();
        Error error = _peer.CreateServer(port, maxPlayers);

        if (error != Error.Ok)
        {
            GD.PushError($"[NetworkServer] Port {port} dinlənilə bilmədi: {error}");
            return error;
        }

        // Disconnect-i poll etmək yerinə ENet-in öz siqnalını dinləyirik:
        // GetPeer() ilə yoxlama mövcud olmayan peer üçün ERROR log-u yaradır.
        _peer.PeerDisconnected += OnPeerDisconnected;

        GD.Print($"[NetworkServer] Port {port}, tick={GameConstants.ServerTickRate}, {VersionGate.Describe()}");
        return Error.Ok;
    }

    public override void _Process(double delta)
    {
        if (_peer is null)
        {
            return;
        }

        _peer.Poll();

        while (_peer.GetAvailablePacketCount() > 0)
        {
            int senderId = _peer.GetPacketPeer();
            byte[] packet = _peer.GetPacket();

            try
            {
                HandlePacket(senderId, packet);
            }
            catch (ArgumentException ex)
            {
                // PRD 47 - Bozuk paket = dəyişdirilmiş client ehtimalı.
                GD.PushWarning($"[NetworkServer] Peer {senderId} yanlış paket göndərdi: {ex.Message}");
                Reject(senderId, "malformed_packet");
            }
        }
    }

    private void OnPeerDisconnected(long peerId) => DropPlayer((int)peerId);

    private void HandlePacket(int senderId, byte[] packet)
    {
        MessageType type = PacketCodec.ReadType(packet);
        ReadOnlySpan<byte> payload = PacketCodec.Payload(packet);

        switch (type)
        {
            case MessageType.Handshake:
                HandleHandshake(senderId, payload);
                break;

            case MessageType.InputCommand when _handshaked.ContainsKey(senderId):
                HandleInput(senderId, PacketCodec.DecodeInput(payload));
                break;

            case MessageType.Disconnect:
                DropPlayer(senderId);
                break;

            default:
                // Handshake-dən əvvəl gələn gameplay paketləri sadəcə atılır.
                break;
        }
    }

    private void HandleHandshake(int senderId, ReadOnlySpan<byte> payload)
    {
        (int protocolVersion, string username) = PacketCodec.DecodeHandshake(payload);

        // PRD 102 - Uyğun olmayan client serverə qoşulmamalıdır.
        if (!VersionGate.IsCompatible(protocolVersion))
        {
            GD.Print($"[NetworkServer] Peer {senderId} rədd edildi — protocol {protocolVersion}");
            Reject(senderId, "version_mismatch");
            return;
        }

        // PRD 52 - Username qaydaları.
        username = username.Trim();
        if (username.Length is < GameConstants.UsernameMinLength or > GameConstants.UsernameMaxLength)
        {
            Reject(senderId, "invalid_username");
            return;
        }

        if (_world is null)
        {
            Reject(senderId, "server_not_ready");
            return;
        }

        Team team = _world.Players.Count % 2 == 0 ? Team.Alpha : Team.Bravo;
        ServerPlayer player = _world.AddPlayer(senderId, username, team);
        _handshaked[senderId] = true;

        Send(senderId, PacketCodec.EncodeHandshakeAccepted(player.PeerId, team, _world.ServerTimeMs));
    }

    private void HandleInput(int senderId, InputCommand input)
    {
        if (_world is null || !_world.Players.TryGetValue(senderId, out ServerPlayer? player))
        {
            return;
        }

        player.LatencyMs = _peer?.GetPeer(senderId)?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime) / 2.0 ?? 0;
        player.EnqueueInput(input);

        // PRD 46 - Atəş də sadəcə bir input-dur; nəticəni server hesablayır.
        if (!input.Buttons.HasFlag(InputButtons.PrimaryFire))
        {
            player.ShotsInBurst = 0;
            return;
        }

        ShotResult result = _world.ProcessShot(senderId, AimDirection(input.YawDegrees, input.PitchDegrees));

        if (result is { Accepted: true, VictimPeerId: { } victimId, HealthDamage: > 0 })
        {
            byte[] damagePacket = PacketCodec.EncodeDamage(
                victimId, senderId, result.HitBox, result.HealthDamage, result.Killed);

            Broadcast(damagePacket, reliable: true);
        }
    }

    /// <summary>Yaw/pitch dərəcələrindən Godot konvensiyasında istiqamət vektoru.</summary>
    internal static System.Numerics.Vector3 AimDirection(float yawDegrees, float pitchDegrees)
    {
        float yaw = yawDegrees * MathF.PI / 180f;
        float pitch = pitchDegrees * MathF.PI / 180f;

        float cosPitch = MathF.Cos(pitch);
        return System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(
            -MathF.Sin(yaw) * cosPitch,
            MathF.Sin(pitch),
            -MathF.Cos(yaw) * cosPitch));
    }

    private void OnSnapshotReady(byte[] payload)
    {
        // Snapshot-lar unreliable gedir — köhnə paketi gözləmək gecikmə yaradar (PRD 44).
        // Yük GameWorld-də artıq serializasiya olunub, yalnız başlıq əlavə edilir.
        Broadcast(PacketCodec.Wrap(MessageType.WorldSnapshot, payload), reliable: false);
    }

    private void Broadcast(byte[] packet, bool reliable)
    {
        if (_peer is null)
        {
            return;
        }

        foreach (int peerId in _handshaked.Keys)
        {
            Send(peerId, packet, reliable);
        }
    }

    private void Send(int peerId, byte[] packet, bool reliable = true)
    {
        if (_peer is null)
        {
            return;
        }

        _peer.SetTargetPeer(peerId);
        _peer.TransferMode = reliable
            ? MultiplayerPeer.TransferModeEnum.Reliable
            : MultiplayerPeer.TransferModeEnum.Unreliable;

        _peer.PutPacket(packet);
    }

    private void Reject(int peerId, string reason)
    {
        Send(peerId, PacketCodec.EncodeHandshakeRejected(reason));
        _peer?.DisconnectPeer(peerId);
        DropPlayer(peerId);
    }

    private void DropPlayer(int peerId)
    {
        if (_handshaked.Remove(peerId))
        {
            _world?.RemovePlayer(peerId);
            GD.Print($"[NetworkServer] Peer ayrıldı: {peerId} ({ConnectedPlayers}/{GameConstants.MaxPlayers})");
        }
    }

    public override void _ExitTree()
    {
        if (_peer is not null)
        {
            _peer.PeerDisconnected -= OnPeerDisconnected;
            _peer.Close();
            _peer = null;
        }
    }
}
