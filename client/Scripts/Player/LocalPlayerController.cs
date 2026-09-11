// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Client.Network;
using NaxcivanCS.Client.Weapons;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;
using NumVector3 = System.Numerics.Vector3;

namespace NaxcivanCS.Client.Player;

/// <summary>
/// PRD 11, 43 - Lokal oyunçunun idarəsi: input toplama, client prediction
/// və server reconciliation.
///
/// <para>
/// Prediction dövrəsi:
/// 1. Input yığılır və serverə göndərilir.
/// 2. Eyni input <see cref="MovementSimulation"/> ilə dərhal lokal tətbiq edilir
///    (oyunçu gecikmə hiss etmir).
/// 3. Input tarixçədə saxlanılır.
/// 4. Server snapshot-ı gəldikdə təsdiqlənmiş mövqe ilə müqayisə olunur;
///    fərq həddi aşarsa mövqe düzəldilir və təsdiqlənməmiş input-lar yenidən oynadılır.
/// </para>
/// </summary>
public sealed partial class LocalPlayerController : CharacterBody3D
{
    private readonly List<InputCommand> _unacknowledgedInputs = new();

    private NetworkClient? _network;
    private Camera3D? _camera;
    private MovementState _state;

    private float _yawDegrees;
    private float _pitchDegrees;
    private float _mouseSensitivity = 0.12f;

    /// <summary>Serverin spawn mövqeyi/bucağı tətbiq olunubmu.</summary>
    private bool _spawnApplied;

    private readonly FootstepTracker _footsteps = new();

    private ViewModel? _viewModel;
    private Vector2 _lastMouseDelta;

    /// <summary>
    /// PRD 17 - Serverin verdiyi recoil sapması, YALNIZ vizual.
    ///
    /// <para>
    /// Vacib: bu dəyər <see cref="_yawDegrees"/>/<see cref="_pitchDegrees"/>-ə
    /// əlavə EDİLMİR. Serverə xam mouse bucağı göndərilir, server isə eyni
    /// sapmanı özü əlavə edir. Əks halda recoil iki dəfə tətbiq olunardı.
    /// Oyunçu mouse-u aşağı çəkərək kompensasiya edir — xam bucaq aşağı düşür,
    /// sapma yuxarı qalxır, nəticə hədəfdə qalır (PRD 17).
    /// </para>
    /// </summary>
    private float _visualPunchPitch;
    private float _visualPunchYaw;

    /// <summary>Tarixçədə saxlanan maksimum input — həddindən artıq böyüməsin.</summary>
    private const int MaxUnacknowledgedInputs = 128;

    /// <summary>PRD 43 - Server düzəlişindən sonra neçə dəfə reconcile olduğunu sayır (debug).</summary>
    public int ReconciliationCount { get; private set; }

    /// <summary>Debug: kursor tutulmadan atəş açır (--auto-fire).</summary>
    public bool AutoFire { get; set; }

    public MovementState State => _state;

    public Camera3D? Camera => _camera;

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Grounded;
        FloorSnapLength = 0.3f;

        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = HitScan.PlayerRadius, Height = HitScan.StandingHeight },
            Position = new Vector3(0f, HitScan.StandingHeight / 2f, 0f),
        });

        _camera = new Camera3D
        {
            Name = "Camera",
            Position = new Vector3(0f, HitScan.EyeHeight(crouching: false), 0f),
            Fov = GameConstants.DefaultFov,
            Current = true,
        };
        AddChild(_camera);

        _viewModel = new ViewModel { Name = "ViewModel" };
        _camera.AddChild(_viewModel);

        _state = MovementState.AtSpawn(ToNumerics(GlobalPosition), 0f);

        // Kursor avtomatik tutulmur — oyunçu pəncərəyə klikləyəndə tutulur,
        // Esc ilə buraxılır. Pəncərə açılan kimi kursoru oğurlamaq
        // development zamanı (və alt-tab edəndə) əsəbiləşdiricidir.
    }

    public void Attach(NetworkClient network)
    {
        _network = network;
        network.SnapshotReceived += OnSnapshotReceived;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _lastMouseDelta = motion.Relative;
            _yawDegrees -= motion.Relative.X * _mouseSensitivity;
            _pitchDegrees = Mathf.Clamp(
                _pitchDegrees - (motion.Relative.Y * _mouseSensitivity),
                -MovementSimulation.MaxPitchDegrees,
                MovementSimulation.MaxPitchDegrees);
        }

        // Pəncərəyə klik = kursoru tut (oyuna gir).
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
            && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        // Esc = kursoru burax (alt-tab, pəncərəni bağlamaq üçün).
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        // PRD 27 - Tam buy menyusu hələ yoxdur; B defuse kit sorğusu göndərir.
        // Alışın mümkünlüyünü server qərara alır (PRD 156).
        if (@event.IsActionPressed("buy_menu") && IsPlaying)
        {
            _network?.SendBuyRequest(BuyItem.DefuseKit);
        }
    }

    /// <summary>Kursor tutulmayıbsa oyunçu hələ "oyunda" deyil — input göndərilmir.</summary>
    private bool IsPlaying => AutoFire || Input.MouseMode == Input.MouseModeEnum.Captured;

    public override void _PhysicsProcess(double delta)
    {
        if (_network is null || !_network.IsConnectedToServer)
        {
            return;
        }

        // Kursor buraxılıbsa neytral input göndərilir: server bizi simulyasiya
        // etməyə davam edir, amma təsadüfi hərəkət/atəş baş vermir.
        Vector2 move = IsPlaying && !AutoFire
            ? Input.GetVector("move_left", "move_right", "move_forward", "move_backward")
            : Vector2.Zero;

        InputCommand input = _network.BuildInput(
            -move.Y, move.X, _yawDegrees, _pitchDegrees, IsPlaying ? CollectButtons() : InputButtons.None);

        // 1) Serverə göndər — bu, gameplay-in yeganə girişidir (PRD 46).
        _network.SendInput(input);

        // 2) Dərhal lokal tətbiq et (PRD 43 - prediction).
        ApplyInput(input, (float)delta);

        // 3) Təsdiq gözləyən input-ları saxla.
        _unacknowledgedInputs.Add(input);
        if (_unacknowledgedInputs.Count > MaxUnacknowledgedInputs)
        {
            _unacknowledgedInputs.RemoveAt(0);
        }

        UpdateCameraHeight();

        // PRD 83 - Öz addım səsi. Shift ilə addımlayanda və çömbələndə səssizdir.
        if (_footsteps.Update(ToNumerics(GlobalPosition), (float)delta, IsOnFloor(), _state.IsCrouching))
        {
            EmitSignal(SignalName.Footstep, GlobalPosition);
        }

        // PRD 17 - Recoil vizual olaraq da sönür. Serverin dəqiq dəyəri növbəti
        // atəşdə gələcək; aradakı kadrlarda hamar qayıdış göstərilir.
        float recovery = RecoilCalculator.RecoveryDegreesPerSecond * (float)delta;
        _visualPunchPitch = Mathf.MoveToward(_visualPunchPitch, 0f, recovery);
        _visualPunchYaw = Mathf.MoveToward(_visualPunchYaw, 0f, recovery);

        _viewModel?.ApplySway(_lastMouseDelta, (float)delta);
        _lastMouseDelta = _lastMouseDelta.Lerp(Vector2.Zero, 12f * (float)delta);
    }

    /// <summary>Eyni simulyasiya + mühərrik kolliziyası — serverlə eyni ardıcıllıq.</summary>
    private void ApplyInput(InputCommand input, float delta)
    {
        _state = MovementSimulation.Step(_state, input, delta);

        Velocity = ToGodot(_state.Velocity);
        MoveAndSlide();

        _state.Position = ToNumerics(GlobalPosition);
        _state.Velocity = ToNumerics(Velocity);
        _state.IsGrounded = IsOnFloor();

        // Bədən xam yaw-a, kamera isə xam + recoil sapmasına baxır.
        Rotation = new Vector3(0f, Mathf.DegToRad(_state.Yaw - _visualPunchYaw), 0f);

        if (_camera is not null)
        {
            _camera.Rotation = new Vector3(Mathf.DegToRad(_state.Pitch + _visualPunchPitch), 0f, 0f);
        }
    }

    /// <summary>PRD 17 - Serverdən gələn atəş: kamera kick + view model geri-təpmə.</summary>
    public void OnShotFired(float punchPitch, float punchYaw)
    {
        _visualPunchPitch = punchPitch;
        _visualPunchYaw = punchYaw;
        _viewModel?.OnFired();
    }

    public void SetReloading(bool reloading) => _viewModel?.SetReloading(reloading);

    /// <summary>Muzzle-ın dünya mövqeyi — tracer buradan başlayır.</summary>
    public Vector3 MuzzleWorldPosition =>
        _viewModel?.MuzzleWorldPosition ?? GlobalPosition + new Vector3(0f, HitScan.EyeHeight(false), 0f);

    /// <summary>PRD 83 - Oyunçu öz addım səsini eşidir.</summary>
    [Signal]
    public delegate void FootstepEventHandler(Vector3 position);

    /// <summary>Recoil-in maksimuma nisbəti — crosshair açılması üçün (PRD 79).</summary>
    public float RecoilRatio => Mathf.Clamp(
        Mathf.Sqrt((_visualPunchPitch * _visualPunchPitch) + (_visualPunchYaw * _visualPunchYaw)) / 12f, 0f, 1f);

    /// <summary>PRD 43 - Server reconciliation.</summary>
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
        catch (ArgumentException)
        {
            return;
        }

        PlayerSnapshot? mine = null;
        foreach (PlayerSnapshot player in snapshot.Players)
        {
            if (player.PeerId == _network.LocalPeerId)
            {
                mine = player;
                break;
            }
        }

        if (mine is not { } authoritative)
        {
            return;
        }

        // İlk snapshot: serverin verdiyi spawn mövqeyini və baxış istiqamətini
        // olduğu kimi qəbul et. Bunsuz oyunçu (0,0,0)-da, yaw=0 ilə başlayır —
        // yəni xəritənin içində, divara baxaraq. Aim bundan sonra tamamilə
        // client-in əlindədir (server mouse ilə mübarizə aparmamalıdır).
        if (!_spawnApplied)
        {
            _spawnApplied = true;

            _yawDegrees = authoritative.Yaw;
            _pitchDegrees = authoritative.Pitch;

            _state.Position = authoritative.Position;
            _state.Yaw = authoritative.Yaw;
            _state.Pitch = authoritative.Pitch;

            GlobalPosition = ToGodot(authoritative.Position);
            Rotation = new Vector3(0f, Mathf.DegToRad(authoritative.Yaw), 0f);

            _unacknowledgedInputs.Clear();
            _footsteps.Reset();
            return;
        }

        // Serverin artıq emal etdiyi input-lar tarixçədən çıxarılır.
        _unacknowledgedInputs.RemoveAll(input => input.Sequence <= authoritative.LastProcessedSequence);

        if (!MovementSimulation.NeedsReconciliation(_state.Position, authoritative.Position))
        {
            return;
        }

        // Serverin dediyi mövqeyə qayıt, sonra YALNIZ təsdiqlənməmiş input-ları yenidən oynat.
        ReconciliationCount++;
        GlobalPosition = ToGodot(authoritative.Position);
        _state.Position = authoritative.Position;
        _state.LastProcessedSequence = authoritative.LastProcessedSequence;

        float tickDelta = GameConstants.ServerTickIntervalSeconds;
        foreach (InputCommand pending in _unacknowledgedInputs)
        {
            ApplyInput(pending, tickDelta);
        }
    }

    private void UpdateCameraHeight()
    {
        if (_camera is null)
        {
            return;
        }

        float target = HitScan.EyeHeight(_state.IsCrouching);
        _camera.Position = new Vector3(0f, Mathf.Lerp(_camera.Position.Y, target, 0.35f), 0f);
    }

    private InputButtons CollectButtons()
    {
        InputButtons buttons = InputButtons.None;

        if (Input.IsActionPressed("jump"))
        {
            buttons |= InputButtons.Jump;
        }

        if (Input.IsActionPressed("crouch"))
        {
            buttons |= InputButtons.Crouch;
        }

        if (Input.IsActionPressed("walk"))
        {
            buttons |= InputButtons.Walk;
        }

        if (AutoFire || Input.IsMouseButtonPressed(MouseButton.Left))
        {
            buttons |= InputButtons.PrimaryFire;
        }

        if (Input.IsActionPressed("reload"))
        {
            buttons |= InputButtons.Reload;
        }

        if (Input.IsActionPressed("scoreboard"))
        {
            buttons |= InputButtons.Scoreboard;
        }

        return buttons;
    }

    internal static Vector3 ToGodot(NumVector3 value) => new(value.X, value.Y, value.Z);

    internal static NumVector3 ToNumerics(Vector3 value) => new(value.X, value.Y, value.Z);
}
