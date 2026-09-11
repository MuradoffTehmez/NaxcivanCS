// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Server.Damage;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Shared.AntiCheat;
using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;
using NumVector3 = System.Numerics.Vector3;

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

    /// <summary>
    /// PRD 9, 10, 128 - Round/match idarəçisi.
    ///
    /// Round məntiqi burada deyil, shared-dəki saf state machine-dədir —
    /// beləliklə round keçidləri unit testlə yoxlanıla bilir.
    /// </summary>
    private readonly MatchDirector _match = new();

    /// <summary>
    /// PRD 8 - Bomba objective-i. Round idarəçisi kimi saf shared state machine;
    /// plant/defuse qaydaları burada deyil, unit testlə örtülmüş sinifdədir.
    /// </summary>
    private readonly BombDirector _bomb = new();

    /// <summary>Hər tick yenidən doldurulur — allocation-dan qaçmaq üçün sahədədir.</summary>
    private readonly List<BombInteractor> _bombInteractors = new();

    /// <summary>Snapshot yayım tezliyi — hər N tick-də bir (64 tick / 2 = 32 Hz).</summary>
    private const int SnapshotEveryNTicks = 2;

    /// <summary>PRD 8 - Plant/defuse progress bar tezliyi (64 tick / 6 ≈ 10 Hz).</summary>
    private const int BombProgressEveryNTicks = 6;

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
        // PRD 8 - Daşıyıcı ayrılarsa bomba yerdə qalmalıdır; əks halda o round
        // ərzində heç kim plant edə bilməz.
        bool bombDropped = _players.TryGetValue(peerId, out ServerPlayer? leaving)
            && peerId == _bomb.CarrierPeerId
            && _bomb.OnCarrierDied(leaving.Movement.Position) != BombEvent.None;

        _players.Remove(peerId);

        if (_bodies.Remove(peerId, out CharacterBody3D? body))
        {
            body.QueueFree();
        }

        if (bombDropped)
        {
            SyncBombOwnership();
            BroadcastBombState();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _tick++;
        _serverTimeMs += delta * 1000.0;

        var fixedDelta = (float)delta;

        _bombInteractors.Clear();

        foreach (ServerPlayer player in _players.Values)
        {
            // PRD 10 - Freeze time-da hərəkət bloklanır. Baxış bucağı yenə
            // işləyir, yalnız yerdəyişmə dayandırılır.
            InputButtons buttons = SimulatePlayer(player, fixedDelta, _match.MovementLocked);

            // PRD 14, 17 - Silah tick-i. Round aktiv deyilsə atəş düyməsi
            // nəzərə alınmır: freeze/buy fazasında atəş açmaq olmaz.
            InputButtons weaponButtons = _match.CombatEnabled
                ? buttons
                : buttons & ~InputButtons.PrimaryFire;

            TickWeapon(player, weaponButtons, fixedDelta);

            // PRD 8 - Bomba qərarı üçün bu tick-dəki niyyət.
            _bombInteractors.Add(new BombInteractor(
                player.PeerId,
                player.State.Team,
                player.State.IsAlive,
                player.Movement.Position,
                player.IsHolding(InputButtons.Interact, _serverTimeMs, GameConstants.HeldInputGraceMs),
                player.State.HasDefuseKit));

            // PRD 45 - Lag compensation üçün mövqe tarixçəsi.
            player.History.Record(_serverTimeMs, player.Movement.Position, player.Movement.Yaw, player.Movement.IsCrouching);
        }

        // Bomba match-dən ƏVVƏL tick edilir: eyni tick-də həm plant tamamlanıb,
        // həm round vaxtı bitibsə, plant üstün gəlməlidir (PRD 8).
        TickBomb(fixedDelta);
        TickMatch(fixedDelta);

        if (_tick % SnapshotEveryNTicks == 0)
        {
            BroadcastSnapshot();
        }
    }

    /// <summary>
    /// PRD 9, 10 - Round/match vəziyyətini irəli aparır və hadisələri tətbiq edir.
    /// </summary>
    private void TickMatch(float delta)
    {
        ApplyMatchEvent(_match.Tick(
            delta, CountAlive(Team.Alpha), CountAlive(Team.Bravo), _players.Count));
    }

    /// <summary>
    /// PRD 9, 10 - Match hadisəsini tətbiq edir.
    ///
    /// Hadisə həm taymer tick-indən, həm də bomba plant/defuse-undan gələ bilər,
    /// ona görə emal tək yerdədir: əks halda defuse roundunda round pulu
    /// paylanmaz, plant-dan sonra isə yeni faza client-lərə göndərilməzdi.
    /// </summary>
    private void ApplyMatchEvent(MatchEvent result)
    {
        switch (result)
        {
            case MatchEvent.None:
                return;

            case MatchEvent.HalftimeSwap:
                SwapTeams();
                StartNewRound();
                break;

            case MatchEvent.RoundStarted:
                StartNewRound();
                break;

            case MatchEvent.RoundEnded:
                // PRD 8 - Bomba taymeri round fazası ilə idarə olunur; faza
                // bitib roundu BombExploded ilə bağlayıbsa bombanı da bağlayırıq.
                if (_match.LastRoundEndReason == RoundEndReason.BombExploded
                    && _bomb.OnTimerExpired() != BombEvent.None)
                {
                    AwardBombExplosion();
                    BroadcastBombState();
                }

                ApplyRoundPayout();
                GD.Print($"[Match] Round {_match.RoundNumber}: {_match.RoundWinner} qazandi " +
                         $"({_match.LastRoundEndReason}) — {_match.AlphaScore}:{_match.BravoScore}");
                EmitSignal(SignalName.ScoreboardChanged);
                break;

            case MatchEvent.MatchEnded:
                GD.Print($"[Match] Match bitdi: {_match.MatchWinner} " +
                         $"({_match.AlphaScore}:{_match.BravoScore})");
                EmitSignal(SignalName.ScoreboardChanged);
                break;

            case MatchEvent.PhaseChanged:
            default:
                break;
        }

        BroadcastRoundState();
    }

    /// <summary>
    /// PRD 8 - Bomba tick-i və onun match-ə təsiri.
    ///
    /// Plant round taymerini bomba taymeri ilə əvəz edir, defuse isə roundu
    /// müdafiənin xeyrinə bitirir. Hər iki halda economy mükafatı verilir.
    /// </summary>
    private void TickBomb(float delta)
    {
        BombEvent result = _bomb.Tick(delta, _match.Phase, _bombInteractors);

        switch (result)
        {
            case BombEvent.Planted:
                SyncBombOwnership();
                AwardPlant();
                ApplyMatchEvent(_match.OnBombPlanted());
                GD.Print($"[Bomb] {_bomb.PlantedSite} site-da yerlesdirildi (peer {_bomb.PlanterPeerId})");
                EmitSignal(SignalName.ScoreboardChanged);
                break;

            case BombEvent.Defused:
                AwardDefuse();
                GD.Print($"[Bomb] Zererzizlesdirildi (peer {_bomb.DefuserPeerId})");
                ApplyMatchEvent(_match.OnBombDefused());
                break;

            case BombEvent.PickedUp:
                SyncBombOwnership();
                break;

            case BombEvent.None:
                return;

            case BombEvent.PlantProgressed:
            case BombEvent.DefuseProgressed:
                // İrəliləyiş yalnız progress bar üçündür; hər tick-də reliable
                // paket göndərmək lazım deyil, ~10 Hz kifayətdir.
                if (_tick % BombProgressEveryNTicks == 0)
                {
                    BroadcastBombState();
                }

                return;

            default:
                break;
        }

        BroadcastBombState();
    }

    /// <summary>PRD 25 - Plant mükafatı: komandaya və yerləşdirən oyunçuya.</summary>
    private void AwardPlant()
    {
        foreach (ServerPlayer player in _players.Values)
        {
            if (player.State.Team != Team.Alpha)
            {
                continue;
            }

            int reward = GameConstants.BombPlantTeamReward
                + (player.PeerId == _bomb.PlanterPeerId ? GameConstants.BombPlantPlayerReward : 0);
            player.State.Money = EconomyRules.AddMoney(player.State.Money, reward);
        }
    }

    /// <summary>PRD 25 - Bomba partlayarsa hücum edən komanda əlavə mükafat alır.</summary>
    private void AwardBombExplosion()
    {
        foreach (ServerPlayer player in _players.Values)
        {
            if (player.State.Team == Team.Alpha)
            {
                player.State.Money = EconomyRules.AddMoney(
                    player.State.Money, GameConstants.BombExplodedTeamReward);
            }
        }
    }

    /// <summary>PRD 25 - Defuse mükafatı yalnız zərərsizləşdirən oyunçuya.</summary>
    private void AwardDefuse()
    {
        if (_players.TryGetValue(_bomb.DefuserPeerId, out ServerPlayer? defuser))
        {
            defuser.State.Money = EconomyRules.AddMoney(
                defuser.State.Money, GameConstants.BombDefusePlayerReward);
        }
    }

    /// <summary>Yalnız cari daşıyıcıda <c>HasBomb</c> qalır.</summary>
    private void SyncBombOwnership()
    {
        foreach (ServerPlayer player in _players.Values)
        {
            player.State.HasBomb = player.PeerId == _bomb.CarrierPeerId;
        }
    }

    /// <summary>PRD 8 - Round başında bomba hücum edən ilk oyunçuya verilir.</summary>
    private void AssignBombCarrier()
    {
        ServerPlayer? carrier = _players.Values
            .Where(p => p.State.Team == Team.Alpha)
            .OrderBy(p => p.PeerId)
            .FirstOrDefault();

        _bomb.BeginRound(
            carrier?.PeerId ?? 0,
            carrier?.Movement.Position ?? BlockoutMap.SpawnPosition(Team.Alpha, 0));

        SyncBombOwnership();
        BroadcastBombState();
    }

    private void BroadcastBombState() => EmitSignal(
        SignalName.BombStateChanged,
        (int)_bomb.State,
        _bomb.CarrierPeerId,
        new Vector3(_bomb.Position.X, _bomb.Position.Y, _bomb.Position.Z),
        _bomb.PlantProgress,
        _bomb.DefuseProgress,
        _bomb.PlantedSite);

    [Signal]
    public delegate void BombStateChangedEventHandler(
        int state, int carrierPeerId, Vector3 position, float plantProgress,
        float defuseProgress, string plantedSite);

    /// <summary>PRD 10 - Yeni round: hamı dirilir, silahlar dolur, mövqelər sıfırlanır.</summary>
    private void StartNewRound()
    {
        foreach (ServerPlayer player in _players.Values)
        {
            player.SpawnPosition = SpawnPointForRound(player.State.Team, player.PeerId);
            player.SpawnYaw = BlockoutMap.SpawnYaw(player.State.Team);
            player.Respawn();
            SyncBody(player);
        }

        _match.RegisterRoundRoster(CountTeam(Team.Alpha), CountTeam(Team.Bravo));
        AssignBombCarrier();
        EmitSignal(SignalName.ScoreboardChanged);

        GD.Print($"[Match] Round {_match.RoundNumber} basladi " +
                 $"({CountTeam(Team.Alpha)}v{CountTeam(Team.Bravo)})");
    }

    /// <summary>PRD 9 - Yarı vaxtda oyunçular tərəf dəyişir.</summary>
    private void SwapTeams()
    {
        foreach (ServerPlayer player in _players.Values)
        {
            player.State.Team = player.State.Team == Team.Alpha ? Team.Bravo : Team.Alpha;
        }

        GD.Print("[Match] Yari vaxt — komandalar teref deyisdi");
    }

    /// <summary>PRD 25, 26 - Round sonu pulu.</summary>
    private void ApplyRoundPayout()
    {
        RoundPayout payout = _match.LastPayout;

        foreach (ServerPlayer player in _players.Values)
        {
            int reward = player.State.Team == Team.Alpha ? payout.AlphaReward : payout.BravoReward;
            player.State.Money = EconomyRules.AddMoney(player.State.Money, reward);
        }
    }

    private int CountAlive(Team team)
        => _players.Values.Count(p => p.State.Team == team && p.State.IsAlive);

    private int CountTeam(Team team)
        => _players.Values.Count(p => p.State.Team == team);

    /// <summary>Komanda daxilində sabit spawn sırası (peer id-yə görə).</summary>
    private NumVector3 SpawnPointForRound(Team team, int peerId)
    {
        int index = 0;
        foreach (ServerPlayer other in _players.Values.Where(p => p.State.Team == team).OrderBy(p => p.PeerId))
        {
            if (other.PeerId == peerId)
            {
                break;
            }

            index++;
        }

        return BlockoutMap.SpawnPosition(team, index);
    }

    private void BroadcastRoundState() => EmitSignal(
        SignalName.RoundStateChanged,
        (int)_match.Phase,
        (int)_match.State,
        _match.PhaseTimeRemaining,
        _match.RoundNumber,
        _match.AlphaScore,
        _match.BravoScore,
        (int)_match.RoundWinner,
        (int)_match.LastRoundEndReason);

    [Signal]
    public delegate void RoundStateChangedEventHandler(
        int phase, int matchState, float timeRemaining, int roundNumber,
        int alphaScore, int bravoScore, int roundWinner, int endReason);

    [Signal]
    public delegate void ScoreboardChangedEventHandler();

    /// <summary>PRD 128 - Scoreboard sətirləri.</summary>
    public IReadOnlyList<PacketCodec.ScoreboardEntry> BuildScoreboard()
        => _players.Values
            .OrderByDescending(p => p.State.Kills)
            .ThenBy(p => p.State.Deaths)
            .Select(p => new PacketCodec.ScoreboardEntry(
                p.PeerId, p.State.Username, p.State.Team,
                p.State.Kills, p.State.Deaths, p.State.Money, p.State.IsAlive))
            .ToList();

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

            // PRD 8 - Daşıyıcı öldükdə bomba öldüyü yerə düşür və götürülə bilər.
            if (victimId == _bomb.CarrierPeerId && _bomb.OnCarrierDied(victim.Movement.Position) != BombEvent.None)
            {
                SyncBombOwnership();
                BroadcastBombState();
            }
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
    private InputButtons SimulatePlayer(ServerPlayer player, float delta, bool movementLocked)
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
            player.RecordProcessedInput(input, _serverTimeMs);

            // PRD 10 - Freeze time: baxış bucağı yenilənir, yerdəyişmə yox.
            if (movementLocked)
            {
                player.Movement.Yaw = input.YawDegrees;
                player.Movement.Pitch = Math.Clamp(input.PitchDegrees, -89f, 89f);
                player.Movement.Velocity = NumVector3.Zero;
                player.Movement.LastProcessedSequence = input.Sequence;
                continue;
            }

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
