using Godot;
using NumVector3 = System.Numerics.Vector3;
using NaxcivanCS.Server.Damage;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Shared.AntiCheat;
using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Server.ServerCore;

/// <summary>
/// PRD 39, 42 - Serverin authoritative simulyasiya dövrəsi.
///
/// Hər tick-də:
/// input emalı → movement → kolliziya → lag-comp tarixçəsi → snapshot yayımı.
/// Gameplay nəticələrinin hamısı burada hesablanır (PRD 156).
/// </summary>
public sealed partial class GameWorld : Node3D
{
    private readonly Dictionary<int, ServerPlayer> _players = new();
    private readonly Dictionary<int, CharacterBody3D> _bodies = new();

    private WeaponCatalog _weapons = new();
    private HitValidator? _hitValidator;
    private uint _tick;
    private double _serverTimeMs;

    /// <summary>PRD 127 - Prototype-da sadə respawn gecikməsi.</summary>
    private const double RespawnDelayMs = 3000;

    /// <summary>Snapshot yayım tezliyi — hər N tick-də bir (64 tick / 2 = 32 Hz).</summary>
    private const int SnapshotEveryNTicks = 2;

    public IReadOnlyDictionary<int, ServerPlayer> Players => _players;

    public double ServerTimeMs => _serverTimeMs;

    public override void _Ready()
    {
        _weapons = LoadWeaponCatalog();

        WeaponData? rifle = _weapons.Get("weapon_rifle_01");
        if (rifle is null)
        {
            GD.PushError("[GameWorld] weapon_rifle_01 config tapılmadı — config/weapons yoxlayın.");
            return;
        }

        _hitValidator = new HitValidator(rifle);
        GD.Print($"[GameWorld] {_weapons.All.Count} silah yükləndi, tick={GameConstants.ServerTickRate}");
    }

    public ServerPlayer AddPlayer(int peerId, string username, Team team)
    {
        NumVector3 spawn = NextSpawnPoint(team);
        var player = new ServerPlayer(peerId, username, team, spawn, BlockoutMap.SpawnYaw(team));
        _players[peerId] = player;

        CharacterBody3D body = CreateBody(peerId, spawn);
        _bodies[peerId] = body;
        AddChild(body);

        GD.Print($"[GameWorld] Oyunçu əlavə edildi: {username} (peer {peerId}, {team})");
        return player;
    }

    public void RemovePlayer(int peerId)
    {
        _players.Remove(peerId);

        if (_bodies.Remove(peerId, out CharacterBody3D? body))
        {
            body.QueueFree();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _tick++;
        _serverTimeMs += delta * 1000.0;

        var fixedDelta = (float)delta;

        foreach (ServerPlayer player in _players.Values)
        {
            SimulatePlayer(player, fixedDelta);

            // PRD 45 - Lag compensation üçün mövqe tarixçəsi.
            player.History.Record(_serverTimeMs, player.Movement.Position, player.Movement.Yaw, player.Movement.IsCrouching);

            if (player.ShouldRespawn(_serverTimeMs, RespawnDelayMs))
            {
                player.SpawnPosition = NextSpawnPoint(player.State.Team);
                player.Respawn();
                SyncBody(player);
            }
        }

        if (_tick % SnapshotEveryNTicks == 0)
        {
            BroadcastSnapshot();
        }
    }

    /// <summary>
    /// PRD 46 - Atəş emalı. Client yalnız istiqamət göndərir, nəticəni server hesablayır.
    /// </summary>
    public ShotResult ProcessShot(int shooterPeerId, NumVector3 aimDirection)
    {
        if (_hitValidator is null || !_players.TryGetValue(shooterPeerId, out ServerPlayer? shooter))
        {
            return default;
        }

        ShotResult result = _hitValidator.Evaluate(shooter, aimDirection, _players.Values, _serverTimeMs);

        if (result.Violation is { } violation)
        {
            RegisterViolation(shooter, violation);
            return result;
        }

        if (result.VictimPeerId is not { } victimId || !_players.TryGetValue(victimId, out ServerPlayer? victim))
        {
            return result;
        }

        victim.ApplyDamage(result.HealthDamage, result.ArmorDamage, _serverTimeMs);
        shooter.State.DamageDealt += result.HealthDamage;

        if (result.HitBox == HitBox.Head)
        {
            shooter.State.Headshots++;
        }

        if (result.Killed)
        {
            shooter.State.Kills++;
            GD.Print($"[GameWorld] {shooter.State.Username} → {victim.State.Username} ({result.HitBox})");
        }

        return result;
    }

    /// <summary>PRD 48 - Pozuntu bal toplayır; avtomatik permanent ban yoxdur.</summary>
    public static void RegisterViolation(ServerPlayer player, Violation violation)
    {
        ArgumentNullException.ThrowIfNull(player);

        player.State.SuspicionScore = SuspicionRules.Apply(player.State.SuspicionScore, violation);
        SuspicionAction action = SuspicionRules.Evaluate(player.State.SuspicionScore);

        if (action != SuspicionAction.None)
        {
            GD.Print($"[AntiCheat] {player.State.Username}: {violation} → bal {player.State.SuspicionScore} ({action})");
        }
    }

    private void SimulatePlayer(ServerPlayer player, float delta)
    {
        if (!player.State.IsAlive)
        {
            return;
        }

        if (!_bodies.TryGetValue(player.PeerId, out CharacterBody3D? body))
        {
            return;
        }

        foreach (InputCommand input in player.DequeueInputsForTick())
        {
            NumVector3 before = player.Movement.Position;

            // PRD 43 - Client ilə EYNİ simulyasiya kodu.
            player.Movement = MovementSimulation.Step(player.Movement, input, delta);

            // Kolliziyanı mühərrik həll edir, nəticə yenidən authoritative vəziyyətə yazılır.
            body.Velocity = ToGodot(player.Movement.Velocity);
            body.MoveAndSlide();

            player.Movement.Position = ToNumerics(body.GlobalPosition);
            player.Movement.Velocity = ToNumerics(body.Velocity);
            player.Movement.IsGrounded = body.IsOnFloor();

            // PRD 47 - Mümkün olmayan yerdəyişmə yoxlaması.
            float moved = NumVector3.Distance(before, player.Movement.Position);
            if (ServerValidators.IsImpossibleMovement(moved, MovementSimulation.MaxPossibleSpeed, delta))
            {
                RegisterViolation(player, Violation.ImpossibleMovement);
                player.Movement.Position = before;
                body.GlobalPosition = ToGodot(before);
            }
        }
    }

    private void BroadcastSnapshot()
    {
        if (_players.Count == 0)
        {
            return;
        }

        var players = new List<PlayerSnapshot>(_players.Count);
        foreach (ServerPlayer player in _players.Values)
        {
            players.Add(player.ToSnapshot());
        }

        var snapshot = new WorldSnapshot(_tick, _serverTimeMs, players);
        byte[] payload = SnapshotSerializer.Serialize(snapshot);

        EmitSignal(SignalName.SnapshotReady, payload);
    }

    [Signal]
    public delegate void SnapshotReadyEventHandler(byte[] payload);

    private void SyncBody(ServerPlayer player)
    {
        if (_bodies.TryGetValue(player.PeerId, out CharacterBody3D? body) && body.IsInsideTree())
        {
            body.GlobalPosition = ToGodot(player.Movement.Position);
            body.Velocity = Vector3.Zero;
        }
    }

    private NumVector3 NextSpawnPoint(Team team)
    {
        int index = _players.Count(p => p.Value.State.Team == team);
        return BlockoutMap.SpawnPosition(team, index);
    }

    private static CharacterBody3D CreateBody(int peerId, NumVector3 spawn)
    {
        // Node hələ səhnə ağacında deyil — GlobalPosition burada işləmir,
        // lokal Position istifadə olunur (GameWorld başlanğıcdadır).
        var body = new CharacterBody3D
        {
            Name = $"Player_{peerId}",
            Position = ToGodot(spawn),
            MotionMode = CharacterBody3D.MotionModeEnum.Grounded,
            FloorSnapLength = 0.3f,
        };

        var shape = new CapsuleShape3D
        {
            Radius = HitScan.PlayerRadius,
            Height = HitScan.StandingHeight,
        };

        body.AddChild(new CollisionShape3D
        {
            Shape = shape,
            Position = new Vector3(0f, HitScan.StandingHeight / 2f, 0f),
        });

        return body;
    }

    private static WeaponCatalog LoadWeaponCatalog()
    {
        // Export edilmiş build-də config res:// altındadır, development-də repo kökündə.
        string[] candidates =
        {
            ProjectSettings.GlobalizePath("res://../config/weapons"),
            ProjectSettings.GlobalizePath("user://config/weapons"),
            Path.Combine(AppContext.BaseDirectory, "config", "weapons"),
        };

        foreach (string path in candidates)
        {
            WeaponCatalog catalog = WeaponCatalog.LoadFromDirectory(path);
            if (catalog.All.Count > 0)
            {
                return catalog;
            }
        }

        return new WeaponCatalog();
    }

    internal static Vector3 ToGodot(NumVector3 value) => new(value.X, value.Y, value.Z);

    internal static NumVector3 ToNumerics(Vector3 value) => new(value.X, value.Y, value.Z);
}
