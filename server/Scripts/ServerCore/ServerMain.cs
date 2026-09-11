using Godot;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Server.ServerCore;

/// <summary>
/// PRD 87, 99 - Linux headless rejimində işləyən dedicated server giriş nöqtəsi.
///
/// İstifadə:
/// <c>godot --headless --path server -- --port 27015 --map NC_Qala</c>
/// </summary>
public sealed partial class ServerMain : Node3D
{
    private Network.NetworkServer? _network;
    private GameWorld? _world;

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        int port = ParseInt(args, "--port") ?? GameConstants.DefaultServerPort;
        string map = ParseString(args, "--map") ?? "NC_Qala";

        GD.Print($"NaxcivanCS Dedicated Server — {VersionBanner()}");
        GD.Print($"[ServerMain] map={map}");

        BuildBlockoutMap();

        _world = new GameWorld { Name = "GameWorld" };
        AddChild(_world);

        _network = new Network.NetworkServer { Name = "NetworkServer" };
        AddChild(_network);
        _network.Attach(_world);

        if (_network.Listen(port) != Error.Ok)
        {
            GetTree().Quit(1);
            return;
        }

        // PRD 137 - Smoke test rejimi: CI-də serverin qalxdığını yoxlamaq üçün.
        if (ParseInt(args, "--smoke-test-seconds") is { } seconds)
        {
            RunSmokeTest(seconds);
        }
    }

    /// <summary>
    /// PRD 127 - Prototype block-out: döşəmə + bir neçə örtük divarı.
    /// Həqiqi xəritələr (NC_Qala və s.) Phase 2-də .tscn kimi gələcək.
    /// </summary>
    private void BuildBlockoutMap()
    {
        AddStaticBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f));

        AddStaticBox("Cover_A", new Vector3(-5f, 0.9f, -4f), new Vector3(3f, 1.8f, 1f));
        AddStaticBox("Cover_B", new Vector3(5f, 0.9f, 4f), new Vector3(3f, 1.8f, 1f));
        AddStaticBox("Cover_Mid", new Vector3(0f, 1.4f, 0f), new Vector3(1f, 2.8f, 6f));

        AddStaticBox("Wall_North", new Vector3(0f, 2f, -20f), new Vector3(40f, 4f, 1f));
        AddStaticBox("Wall_South", new Vector3(0f, 2f, 20f), new Vector3(40f, 4f, 1f));
        AddStaticBox("Wall_East", new Vector3(20f, 2f, 0f), new Vector3(1f, 4f, 40f));
        AddStaticBox("Wall_West", new Vector3(-20f, 2f, 0f), new Vector3(1f, 4f, 40f));
    }

    private void AddStaticBox(string name, Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D { Name = name, Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }

    private void RunSmokeTest(int seconds)
    {
        GD.Print($"[ServerMain] Smoke test: {seconds} saniyə sonra çıxılacaq.");

        var timer = new Godot.Timer { WaitTime = seconds, OneShot = true, Autostart = true };
        timer.Timeout += () =>
        {
            GD.Print($"[ServerMain] Smoke test bitdi — tick loop stabil, oyunçu: {_network?.ConnectedPlayers ?? 0}");
            GetTree().Quit(0);
        };

        AddChild(timer);
    }

    private static string VersionBanner()
        => $"protocol={GameConstants.ProtocolVersion}; game={GameConstants.GameVersion}";

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
