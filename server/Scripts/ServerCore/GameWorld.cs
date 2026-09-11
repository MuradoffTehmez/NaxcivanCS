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

    /// <summary>PRD 136 - MVP-də hamı eyni tüfənglə başlayır; buy menu Phase 3-dədir.</summary>
    private WeaponData DefaultWeapon { get; set; } = new()
    {
        Id = "fallback",
        Name = "Fallback",
        Category = WeaponCategory.Rifle,
        Damage = 34,
        FireRate = 600,
        MagazineSize = 30,
        ReserveAmmo = 90,
        ReloadTime = 2.4f,
        Automatic = true,
    };
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

        DefaultWeapon = rifle;
        _hitValidator = new HitValidator(rifle);
        GD.Print($"[GameWorld] {_weapons.All.Count} silah yükləndi, tick={GameConstants.ServerTickRate}");
    }

    public ServerPlayer AddPlayer(int peerId, string username, Team team)
    {
        NumVector3 spawn = NextSpawnPoint(team);
        var player = new ServerPlayer(peerId, username, team, spawn, BlockoutMap.SpawnYaw(team), DefaultWeapon);
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
            InputButtons buttons = SimulatePlayer(player, fixedDelta);

            // PRD 14, 17 - Silah tick-i: atəş, reload, recoil. Hərəkətdən SONRA
            // çağırılır ki, güllə oyunçunun bu tick-dəki son mövqeyindən çıxsın.
            TickWeapon(player, buttons, fixedDelta);

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
    /// PRD 14, 17, 46 - Silahın bir tick-i: kadensiya, patron, reload, recoil.
    ///
    /// Client yalnız düymə vəziyyətini göndərir; atəşin açılıb-açılmayacağını
    /// <see cref="WeaponRuntime"/> qərara alır. Atəş açılarsa, güllənin
    /// istiqamətinə serverin hesabladığı recoil əlavə olunur — beləliklə
    /// "no-recoil" hiylə mənasızdır (PRD 156).
    /// </summary>
    private void TickWeapon(ServerPlayer player, InputButtons buttons, float delta)
    {
        if (_hitValidator is null || !player.State.IsAlive)
        {
            return;
        }

        bool fireHeld = buttons.HasFlag(InputButtons.PrimaryFire);
        bool reloadRequested = buttons.HasFlag(InputButtons.Reload);

        WeaponAction action = player.Weapon.Tick(fireHeld, reloadRequested, delta);

        if (action is WeaponAction.ReloadStarted or WeaponAction.ReloadFinished)
        {
            EmitSignal(SignalName.WeaponStateChanged, player.PeerId);
            return;
        }

        if (action != WeaponAction.Fire)
        {
            return;
        }

        // Güllə: sonuncu gülləyə tətbiq olunan sapma (ilk atışda sıfır).
        RecoilPunch bulletPunch = player.Weapon.LastShotPunch;
        NumVector3 direction = AimDirection(
            player.Movement.Yaw - bulletPunch.Yaw,
            player.Movement.Pitch + bulletPunch.Pitch);

        // Kamera kick-i: atışdan SONRAKI sapma — oyunçu silahın qalxdığını görür.
        RecoilPunch punch = player.Weapon.Punch;

        ShotResult result = _hitValidator.Evaluate(player, direction, _players.Values, _serverTimeMs);

        EmitSignal(
            SignalName.ShotFired,
            player.PeerId,
            ToGodot(result.Origin),
            ToGodot(result.End),
            punch.Pitch,
            punch.Yaw,
            player.Weapon.ShotIndex - 1,
            result.VictimPeerId is not null);

        EmitSignal(SignalName.WeaponStateChanged, player.PeerId);

        if (result.VictimPeerId is not { } victimId || !_players.TryGetValue(victimId, out ServerPlayer? victim))
        {
            return;
        }

        victim.ApplyDamage(result.HealthDamage, result.ArmorDamage, _serverTimeMs);
        player.State.DamageDealt += result.HealthDamage;

        if (result.HitBox == HitBox.Head)
        {
            player.State.Headshots++;
        }

        if (result.Killed)
        {
            player.State.Kills++;
            GD.Print($"[GameWorld] {player.State.Username} → {victim.State.Username} ({result.HitBox})");
        }

        EmitSignal(
            SignalName.PlayerDamaged,
            victimId,
            player.PeerId,
            (int)result.HitBox,
            result.HealthDamage,
            result.Killed);
    }

    /// <summary>Yaw/pitch dərəcələrindən Godot konvensiyasında istiqamət vektoru.</summary>
    internal static NumVector3 AimDirection(float yawDegrees, float pitchDegrees)
    {
        float yaw = yawDegrees * MathF.PI / 180f;
        float pitch = pitchDegrees * MathF.PI / 180f;

        float cosPitch = MathF.Cos(pitch);
        return NumVector3.Normalize(new NumVector3(
            -MathF.Sin(yaw) * cosPitch,
            MathF.Sin(pitch),
            -MathF.Cos(yaw) * cosPitch));
    }

    [Signal]
    public delegate void ShotFiredEventHandler(
        int shooterPeerId, Vector3 origin, Vector3 end, float punchPitch, float punchYaw, int shotIndex, bool hit);

    [Signal]
    public delegate void PlayerDamagedEventHandler(
        int victimPeerId, int attackerPeerId, int hitBox, int healthDamage, bool killed);

    [Signal]
    public delegate void WeaponStateChangedEventHandler(int peerId);

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

    /// <summary>
    /// Oyunçunun bu tick-dəki input-larını simulyasiya edir və
    /// <b>sonuncu</b> input-un düymə vəziyyətini qaytarır.
    ///
    /// Silah kadensiyası tick başına bir dəfə hesablanır: bir tick-də bir neçə
    /// input emal olunsa belə, silah öz sürətindən tez atəş aça bilməz.
    /// </summary>
    private InputButtons SimulatePlayer(ServerPlayer player, float delta)
    {
        InputButtons lastButtons = InputButtons.None;

        if (!player.State.IsAlive)
        {
            return lastButtons;
        }

        if (!_bodies.TryGetValue(player.PeerId, out CharacterBody3D? body))
        {
            return lastButtons;
        }

        foreach (InputCommand input in player.DequeueInputsForTick())
        {
            lastButtons = input.Buttons;

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

        return lastButtons;
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
