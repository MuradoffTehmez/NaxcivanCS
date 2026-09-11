using Godot;
using NaxcivanCS.Client.Audio;
using NaxcivanCS.Client.Effects;
using NaxcivanCS.Client.Network;
using NaxcivanCS.Client.Player;
using NaxcivanCS.Client.UI;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
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
    private ShotEffects? _effects;
    private GameAudio? _audio;
    private bool _autoFire;
    private bool _wasReloading;

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

        WorldBuilder.Build(this);

        // HUD MUTLEQ CanvasLayer altinda olmalidir: Node3D valideyni Control
        // ucun layout etmir, neticede anchor-lar (0,0) olcuye qarsi hesablanir
        // ve ekranin kenarina baglanan elementler gorunmez qalir.
        var hudLayer = new CanvasLayer { Name = "HudLayer" };
        AddChild(hudLayer);

        _hud = new PrototypeHud { Name = "Hud" };
        hudLayer.AddChild(_hud);
        _hud.UpdateStatus($"{address}:{port} — qoşulur...");

        _network = new NetworkClient { Name = "Network", Username = username };
        AddChild(_network);

        _network.HandshakeAccepted += OnHandshakeAccepted;
        _network.HandshakeRejected += OnHandshakeRejected;
        _network.SnapshotReceived += OnSnapshotReceived;
        _network.DamageReceived += OnDamageReceived;
        _network.ShotFired += OnShotFired;
        _network.WeaponStateReceived += OnWeaponStateReceived;
        _network.Disconnected += OnDisconnected;

        _effects = new ShotEffects { Name = "ShotEffects" };
        AddChild(_effects);

        _audio = new GameAudio { Name = "Audio" };
        AddChild(_audio);

        if (_network.ConnectToServer(address, port) != Error.Ok)
        {
            _hud.UpdateStatus("Serverə qoşulmaq alınmadı");
        }

        // PRD 137 - CI-də end-to-end yoxlama: qoşul, snapshot al, çıx.
        if (ParseInt(args, "--smoke-test-seconds") is { } seconds)
        {
            RunSmokeTest(seconds);
        }

        // Debug: avtomatik atəş — gunplay effektlərini yoxlamaq üçün.
        // Yalnız --auto-fire verildikdə işləyir, normal oyuna təsir etmir.
        _autoFire = ParseString(args, "--auto-fire") is not null;

        // Debug: render yolunun doğru işlədiyini yoxlamaq üçün ekran şəkli.
        if (ParseString(args, "--screenshot") is { } screenshotPath)
        {
            CaptureScreenshotAfter(2.0, screenshotPath);
        }
    }

    /// <summary>
    /// Viewport-u PNG kimi yadda saxlayır. Vizual reqressiyaları avtomatik
    /// yoxlamaq üçün — headless rejimdə işləmir, pəncərə lazımdır.
    /// </summary>
    private void CaptureScreenshotAfter(double delaySeconds, string path)
    {
        var timer = new Godot.Timer { WaitTime = delaySeconds, OneShot = true, Autostart = true };
        timer.Timeout += async () =>
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            Image image = GetViewport().GetTexture().GetImage();
            Error error = image.SavePng(path);

            GD.Print(error == Error.Ok
                ? $"[GameBootstrap] Ekran şəkli: {path} ({image.GetWidth()}x{image.GetHeight()})"
                : $"[GameBootstrap] Ekran şəkli alınmadı: {error}");
        };

        AddChild(timer);
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

        if (_localPlayer is not null)
        {
            _hud?.SetCrosshairSpread(_localPlayer.RecoilRatio);
        }

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
        _localPlayer.AutoFire = _autoFire;
        _localPlayer.Footstep += position => OnFootstep(position, isLocal: true);
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
                view.Footstep += position => OnFootstep(position, isLocal: false);
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

        // PRD 78 - Hit feedback: vurduğunu bilmək gunplay-in yarısıdır.
        if (attackerPeerId == _network.LocalPeerId)
        {
            _hud?.ShowHitMarker(killed);
            _audio?.PlayHitMarker(killed);
        }
    }

    /// <summary>
    /// PRD 17, 46 - Serverin hesabladığı atəş: tracer, muzzle flash, kamera kick.
    /// Effektlər serverin verdiyi nöqtələrdən qurulur — client "hara dəydi"
    /// qərarını özü vermir.
    /// </summary>
    private void OnShotFired(
        int shooterPeerId, Vector3 origin, Vector3 end, float punchPitch, float punchYaw, int shotIndex, bool hit)
    {
        if (_network is null)
        {
            return;
        }

        bool isLocal = shooterPeerId == _network.LocalPeerId;

        // PRD 83 - Vurulma səsi hədəfin yerindən gəlir: "dəydimi?" sualına
        // qulaqla da cavab verir.
        _audio?.PlayImpact(end, hit);

        if (isLocal && _localPlayer is not null)
        {
            _localPlayer.OnShotFired(punchPitch, punchYaw);
            _audio?.PlayLocalShot();

            // Lokal oyunçu üçün tracer view model-in lüləsindən başlayır ki,
            // silahla üst-üstə düşsün.
            _effects?.SpawnTracer(_localPlayer.MuzzleWorldPosition, end, hit);
            return;
        }

        _audio?.PlayRemoteShot(origin);
        _effects?.SpawnTracer(origin, end, hit);
    }

    private void OnWeaponStateReceived(int magazine, int reserve, bool reloading)
    {
        _hud?.UpdateAmmo(magazine, reserve, reloading);
        _localPlayer?.SetReloading(reloading);

        // PRD 83 - Reload səsi yalnız vəziyyət DƏYİŞƏNDƏ çalınır; server
        // hər atəşdə də weapon-state göndərir.
        if (reloading == _wasReloading)
        {
            return;
        }

        _wasReloading = reloading;
        Vector3 position = _localPlayer?.GlobalPosition ?? GlobalPosition;

        if (reloading)
        {
            _audio?.PlayReloadStart(position, isLocal: true);
        }
        else
        {
            _audio?.PlayReloadEnd(position, isLocal: true);
        }
    }

    /// <summary>PRD 83, 84 - Addım səsi ayağın altındakı səthə görə seçilir.</summary>
    private void OnFootstep(Vector3 position, bool isLocal)
    {
        SurfaceMaterial surface = BlockoutMap.SurfaceAt(
            new System.Numerics.Vector3(position.X, position.Y, position.Z));

        _audio?.PlayFootstep(position, surface, isLocal);
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
