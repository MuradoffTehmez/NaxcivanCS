using Godot;

namespace NaxcivanCS.Client.Weapons;

/// <summary>
/// PRD 13, 14 - Birinci şəxs silah view model-i.
///
/// <para>
/// Prototype üçün sadə həndəsi formadır — məqsəd atəşin <b>hiss olunması</b>dır,
/// gözəl model deyil (PRD 151: əvvəlcə gunplay, sonra art). Silah modeli
/// Phase 2-də əvəz olunacaq.
/// </para>
///
/// <para>
/// Funksiyalar: weapon sway (baxış hərəkətindən geridə qalma), atəş geri-təpməsi,
/// reload animasiyası (aşağı-yuxarı) və muzzle flash.
/// </para>
/// </summary>
public sealed partial class ViewModel : Node3D
{
    /// <summary>
    /// Silahın dincəlik mövqeyi: aşağı-sağ künc. Ekranın altından çıxmalıdır
    /// ki, "havada üzən obyekt" kimi görünməsin.
    /// </summary>
    private static readonly Vector3 RestPosition = new(0.25f, -0.315f, -0.5f);
    private static readonly Vector3 ReloadOffset = new(0.06f, -0.22f, 0.05f);

    private const float SwayAmount = 0.014f;
    private const float SwaySpeed = 9f;
    private const float KickbackDistance = 0.055f;
    private const float KickRecoverySpeed = 11f;
    private const float MuzzleFlashSeconds = 0.045f;

    private MeshInstance3D? _body;
    private MeshInstance3D? _barrel;
    private OmniLight3D? _muzzleLight;
    private MeshInstance3D? _muzzleFlash;
    private Node3D? _muzzlePoint;

    private Vector3 _baseRotation;
    private Vector3 _swayOffset;
    private float _kickback;
    private float _flashRemaining;
    private float _reloadProgress;
    private bool _isReloading;

    public override void _Ready()
    {
        Position = RestPosition;

        // Silah bir qədər içəri çevrilir — birinci şəxs perspektivində
        // düz irəli baxan model yastı və süni görünür.
        _baseRotation = new Vector3(0f, Mathf.DegToRad(-3.5f), Mathf.DegToRad(2.5f));
        Rotation = _baseRotation;

        var metal = new StandardMaterial3D { AlbedoColor = new Color("2f3338"), Metallic = 0.7f, Roughness = 0.4f };
        var grip = new StandardMaterial3D { AlbedoColor = new Color("4a3628"), Roughness = 0.85f };

        _body = new MeshInstance3D
        {
            Name = "Body",
            Mesh = new BoxMesh { Size = new Vector3(0.05f, 0.075f, 0.26f) },
            MaterialOverride = metal,
        };
        AddChild(_body);

        _barrel = new MeshInstance3D
        {
            Name = "Barrel",
            Mesh = new BoxMesh { Size = new Vector3(0.025f, 0.025f, 0.24f) },
            Position = new Vector3(0f, 0.022f, -0.24f),
            MaterialOverride = metal,
        };
        AddChild(_barrel);

        AddChild(new MeshInstance3D
        {
            Name = "Grip",
            Mesh = new BoxMesh { Size = new Vector3(0.04f, 0.115f, 0.045f) },
            Position = new Vector3(0f, -0.085f, 0.055f),
            Rotation = new Vector3(Mathf.DegToRad(-12f), 0f, 0f),
            MaterialOverride = grip,
        });

        AddChild(new MeshInstance3D
        {
            Name = "Magazine",
            Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.1f, 0.05f) },
            Position = new Vector3(0f, -0.075f, -0.045f),
            MaterialOverride = metal,
        });

        _muzzlePoint = new Node3D { Name = "MuzzlePoint", Position = new Vector3(0f, 0.022f, -0.37f) };
        AddChild(_muzzlePoint);

        _muzzleFlash = new MeshInstance3D
        {
            Name = "MuzzleFlash",
            Mesh = new SphereMesh { Radius = 0.032f, Height = 0.064f, RadialSegments = 6, Rings = 3 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("ffd27a"),
                EmissionEnabled = true,
                Emission = new Color("ffb43c"),
                EmissionEnergyMultiplier = 6f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
            Visible = false,
        };
        _muzzlePoint.AddChild(_muzzleFlash);

        _muzzleLight = new OmniLight3D
        {
            Name = "MuzzleLight",
            LightColor = new Color("ffc266"),
            LightEnergy = 0f,
            OmniRange = 6f,
        };
        _muzzlePoint.AddChild(_muzzleLight);
    }

    /// <summary>Muzzle-ın dünya mövqeyi — tracer buradan başlayır.</summary>
    public Vector3 MuzzleWorldPosition => _muzzlePoint?.GlobalPosition ?? GlobalPosition;

    /// <summary>Atəş: geri-təpmə + muzzle flash.</summary>
    public void OnFired()
    {
        _kickback = KickbackDistance;
        _flashRemaining = MuzzleFlashSeconds;

        if (_muzzleFlash is not null)
        {
            _muzzleFlash.Visible = true;
            _muzzleFlash.Rotation = new Vector3(0f, 0f, GD.Randf() * Mathf.Tau);
        }

        if (_muzzleLight is not null)
        {
            _muzzleLight.LightEnergy = 3.2f;
        }
    }

    public void SetReloading(bool reloading) => _isReloading = reloading;

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        // Muzzle flash sönməsi.
        if (_flashRemaining > 0f)
        {
            _flashRemaining -= dt;
            if (_flashRemaining <= 0f)
            {
                if (_muzzleFlash is not null)
                {
                    _muzzleFlash.Visible = false;
                }

                if (_muzzleLight is not null)
                {
                    _muzzleLight.LightEnergy = 0f;
                }
            }
            else if (_muzzleLight is not null)
            {
                _muzzleLight.LightEnergy = 3.2f * (_flashRemaining / MuzzleFlashSeconds);
            }
        }

        _kickback = Mathf.Lerp(_kickback, 0f, KickRecoverySpeed * dt);

        // Reload: silah aşağı düşüb qalxır.
        _reloadProgress = Mathf.Lerp(_reloadProgress, _isReloading ? 1f : 0f, 7f * dt);

        Position = RestPosition
            + _swayOffset
            + (ReloadOffset * _reloadProgress)
            + new Vector3(0f, 0f, _kickback);

        Rotation = new Vector3(
            _baseRotation.X + Mathf.DegToRad(_kickback * 45f) + Mathf.DegToRad(_reloadProgress * -28f),
            _baseRotation.Y,
            _baseRotation.Z);
    }

    /// <summary>
    /// PRD 13 - Weapon sway: silah baxış hərəkətindən bir qədər geridə qalır.
    /// </summary>
    public void ApplySway(Vector2 mouseDelta, float delta)
    {
        var target = new Vector3(
            Mathf.Clamp(-mouseDelta.X * SwayAmount, -0.05f, 0.05f),
            Mathf.Clamp(-mouseDelta.Y * SwayAmount, -0.05f, 0.05f),
            0f);

        _swayOffset = _swayOffset.Lerp(target, SwaySpeed * delta);
    }
}
