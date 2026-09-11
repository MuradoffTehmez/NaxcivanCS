using Godot;
using NaxcivanCS.Client.Network;
using NaxcivanCS.Client.Player;
using NaxcivanCS.Client.UI;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.Core;

/// <summary>
/// PRD 152 - Prototype 0.1 giriş nöqtəsi.
///
/// İstifadə:
/// <c>godot --path client -- --server 127.0.0.1 --port 27015 --name Tahmaz</c>
/// </summary>
public sealed partial class GameBootstrap : Node3D
{
    private readonly Dictionary<int, RemotePlayerView> _remotePlayers = new();

    private NetworkClient? _network;
    private LocalPlayerController? _localPlayer;
    private PrototypeHud? _hud;

    private int _snapshotCount;
    private double _lastServerTimeMs;
    private double _renderClockMs;

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string address = ParseString(args, "--server") ?? "127.0.0.1";
        int port = ParseInt(args, "--port") ?? GameConstants.DefaultServerPort;
        string username = ParseString(args, "--name") ?? $"Player{GD.Randi() % 1000}";

        GD.Print($"NaxcivanCS Client — {VersionGate.Describe()}");

        BuildEnvironment();

        _hud = new PrototypeHud { Name = "Hud" };
        AddChild(_hud);
        _hud.UpdateStatus($"{address}:{port} — qoşulur...");

        _network = new NetworkClient { Name = "Network", Username = username };
        AddChild(_network);

        _network.HandshakeAccepted += OnHandshakeAccepted;
        _network.HandshakeRejected += OnHandshakeRejected;
        _network.SnapshotReceived += OnSnapshotReceived;
        _network.DamageReceived += OnDamageReceived;
        _network.Disconnected += OnDisconnected;

        if (_network.ConnectToServer(address, port) != Error.Ok)
        {
            _hud.UpdateStatus("Serverə qoşulmaq alınmadı");
        }

        // PRD 137 - CI-də end-to-end yoxlama: qoşul, snapshot al, çıx.
        if (ParseInt(args, "--smoke-test-seconds") is { } seconds)
        {
            RunSmokeTest(seconds);
        }
    }

    /// <summary>
    /// Headless rejimdə serverə qoşulub snapshot axınının işlədiyini təsdiqləyir.
    /// Snapshot gəlməyibsə sıfırdan fərqli kodla çıxır ki, CI uğursuz olsun.
    /// </summary>
    private void RunSmokeTest(int seconds)
    {
        GD.Print($"[GameBootstrap] Smoke test: {seconds} saniyə.");

        var timer = new Godot.Timer { WaitTime = seconds, OneShot = true, Autostart = true };
        timer.Timeout += () =>
        {
            bool connected = _network?.LocalPeerId > 0;
            bool receivedSnapshots = _snapshotCount > 0;

            GD.Print($"[GameBootstrap] Smoke test nəticəsi: handshake={connected}, " +
                     $"snapshot={_snapshotCount}, ping={_network?.PingMs:F0}ms");

            GetTree().Quit(connected && receivedSnapshots ? 0 : 1);
        };

        AddChild(timer);
    }

    public override void _Process(double delta)
    {
        if (_network is null)
        {
            return;
        }

        _hud?.UpdatePing(_network.PingMs);

        // PRD 44 - Render vaxtı serverin son snapshot-ından bir qədər geridədir.
        _renderClockMs += delta * 1000.0;
        double renderTime = _lastServerTimeMs + _renderClockMs - GameConstants.SnapshotInterpolationDelayMs;

        foreach (RemotePlayerView remote in _remotePlayers.Values)
        {
            remote.Interpolate(renderTime);
        }
    }

    private void OnHandshakeAccepted(int peerId, int team)
    {
        if (_network is null)
        {
            return;
        }

        _hud?.UpdateStatus($"Qoşuldu — komanda: {(Team)team}");

        _localPlayer = new LocalPlayerController { Name = "LocalPlayer" };
        AddChild(_localPlayer);
        _localPlayer.Attach(_network);
    }

    private void OnHandshakeRejected(string reason)
    {
        string message = reason switch
        {
            "version_mismatch" => "Client versiyası serverlə uyğun deyil",
            "invalid_username" => "Username qaydalara uyğun deyil (3-20 simvol)",
            _ => $"Server rədd etdi: {reason}",
        };

        _hud?.UpdateStatus(message);
        GD.PushWarning($"[GameBootstrap] {message}");
    }

    private void OnSnapshotReceived(byte[] payload)
    {
        if (_network is null)
        {
            return;
        }

        WorldSnapshot snapshot;
        try
        {
            snapshot = SnapshotSerializer.Deserialize(payload);
        }
        catch (ArgumentException ex)
        {
            GD.PushWarning($"[GameBootstrap] Snapshot oxunmadı: {ex.Message}");
            return;
        }

        _snapshotCount++;
        _lastServerTimeMs = snapshot.ServerTimeMs;
        _renderClockMs = 0;

        var seen = new HashSet<int>();

        foreach (PlayerSnapshot player in snapshot.Players)
        {
            seen.Add(player.PeerId);

            if (player.PeerId == _network.LocalPeerId)
            {
                _hud?.UpdateHealth(player.Health, player.Armor);
                continue;
            }

            if (!_remotePlayers.TryGetValue(player.PeerId, out RemotePlayerView? view))
            {
                view = RemotePlayerView.Create(player.PeerId);
                _remotePlayers[player.PeerId] = view;
                AddChild(view);
            }

            view.Push(player, snapshot.ServerTimeMs);
        }

        // Snapshot-da olmayan oyunçular serveri tərk edib.
        foreach (int peerId in _remotePlayers.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _remotePlayers[peerId].QueueFree();
            _remotePlayers.Remove(peerId);
        }
    }

    private void OnDamageReceived(int victimPeerId, int attackerPeerId, int hitBox, int healthDamage, bool killed)
    {
        if (_network is null)
        {
            return;
        }

        if (attackerPeerId == _network.LocalPeerId)
        {
            // PRD 78 - Hit feedback. Vizual/audio effekt Phase 2-də əlavə olunacaq.
            GD.Print($"[Hit] {(HitBox)hitBox} — {healthDamage} damage{(killed ? " (kill)" : string.Empty)}");
        }
    }

    private void OnDisconnected()
    {
        _hud?.UpdateStatus("Server ilə əlaqə kəsildi");

        foreach (RemotePlayerView view in _remotePlayers.Values)
        {
            view.QueueFree();
        }

        _remotePlayers.Clear();
    }

    /// <summary>PRD 127 - Prototype block-out; server ilə eyni həndəsə.</summary>
    private void BuildEnvironment()
    {
        AddChild(new DirectionalLight3D
        {
            Name = "Sun",
            Rotation = new Vector3(Mathf.DegToRad(-55f), Mathf.DegToRad(35f), 0f),
            ShadowEnabled = true,
        });

        AddChild(new WorldEnvironment
        {
            Name = "Environment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            },
        });

        AddBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f));
        AddBox("Cover_A", new Vector3(-5f, 0.9f, -4f), new Vector3(3f, 1.8f, 1f));
        AddBox("Cover_B", new Vector3(5f, 0.9f, 4f), new Vector3(3f, 1.8f, 1f));
        AddBox("Cover_Mid", new Vector3(0f, 1.4f, 0f), new Vector3(1f, 2.8f, 6f));
        AddBox("Wall_North", new Vector3(0f, 2f, -20f), new Vector3(40f, 4f, 1f));
        AddBox("Wall_South", new Vector3(0f, 2f, 20f), new Vector3(40f, 4f, 1f));
        AddBox("Wall_East", new Vector3(20f, 2f, 0f), new Vector3(1f, 4f, 40f));
        AddBox("Wall_West", new Vector3(-20f, 2f, 0f), new Vector3(1f, 4f, 40f));
    }

    private void AddBox(string name, Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D { Name = name, Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        body.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size } });
        AddChild(body);
    }

    private static int? ParseInt(string[] args, string flag)
        => int.TryParse(ParseString(args, flag), out int value) ? value : null;

    private static string? ParseString(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
