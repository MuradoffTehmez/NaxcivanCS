// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Gameplay;

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
    /// PRD 127 - Prototype block-out. Həndəsə <see cref="BlockoutMap"/>-dan gəlir —
    /// client ilə eyni mənbə, beləliklə örtüklər iki tərəfdə fərqlənə bilmir.
    /// Serverdə yalnız kolliziya qurulur, mesh lazım deyil (headless).
    /// </summary>
    private void BuildBlockoutMap()
    {
        foreach (MapBlock block in BlockoutMap.Blocks)
        {
            var body = new StaticBody3D
            {
                Name = block.Name,
                Position = new Vector3(block.Center.X, block.Center.Y, block.Center.Z),
            };

            body.AddChild(new CollisionShape3D
            {
                Shape = new BoxShape3D { Size = new Vector3(block.Size.X, block.Size.Y, block.Size.Z) },
            });

            AddChild(body);
        }

        GD.Print($"[ServerMain] Block-out: {BlockoutMap.Blocks.Count} blok");
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
