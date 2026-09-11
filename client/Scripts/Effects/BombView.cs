// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Client.Audio;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Client.Effects;

/// <summary>
/// PRD 8, 84 - Bombanın dünyadakı təsviri və səsi.
///
/// <para>
/// Server yalnız vəziyyəti və mövqeyi göndərir; görüntü və taymer siqnalı
/// burada qurulur. Bomba daşınarkən görünmür — daşıyıcı oyunçu modelinin
/// özüdür; yerə düşəndə və ya yerləşdiriləndə isə görünməlidir, əks halda
/// oyunçu onu tapa bilmir.
/// </para>
///
/// <para>
/// Beep tezliyi partlayışa qalan vaxta görə sürətlənir: bu, müdafiə üçün
/// taktiki məlumatdır (PRD 83) və yalnız client tərəfdə hesablanır, ona görə
/// şəbəkəyə əlavə yük gətirmir.
/// </para>
/// </summary>
public sealed partial class BombView : Node3D
{
    /// <summary>Taymerin başında və sonunda beep arası interval, saniyə.</summary>
    private const float SlowBeepSeconds = 1.05f;
    private const float FastBeepSeconds = 0.12f;

    private static readonly Color CarriedColor = new("d9a441");
    private static readonly Color ArmedColor = new("e0442f");

    private MeshInstance3D? _mesh;
    private OmniLight3D? _light;
    private GameAudio? _audio;

    private BombState _state = BombState.Carried;
    private float _beepCooldown;
    private float _timeSincePlanted;

    public override void _Ready()
    {
        _mesh = new MeshInstance3D
        {
            Name = "BombMesh",
            Mesh = new BoxMesh { Size = new Vector3(0.34f, 0.16f, 0.24f) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = CarriedColor,
                EmissionEnabled = true,
                Emission = CarriedColor,
                EmissionEnergyMultiplier = 0.6f,
                Roughness = 0.6f,
                Metallic = 0.3f,
            },
        };
        AddChild(_mesh);

        // Yerləşdirilmiş bomba qaranlıq küncdə də seçilməlidir.
        _light = new OmniLight3D
        {
            Name = "BombLight",
            LightColor = ArmedColor,
            LightEnergy = 0f,
            OmniRange = 4.5f,
            Position = new Vector3(0f, 0.25f, 0f),
        };
        AddChild(_light);

        Visible = false;
    }

    /// <summary>Səs sistemi bağlanır; verilmədikdə bomba səssiz qalır.</summary>
    public void AttachAudio(GameAudio audio) => _audio = audio;

    /// <summary>
    /// PRD 8 - Serverdən gələn vəziyyət. Yalnız vəziyyət dəyişəndə çağırılır,
    /// hər tick-də deyil.
    /// </summary>
    public void Apply(BombState state, Vector3 position)
    {
        BombState previous = _state;
        _state = state;

        // Daşınan bomba ayrıca obyekt kimi göstərilmir.
        Visible = state is BombState.Dropped or BombState.Planted;

        if (Visible)
        {
            GlobalPosition = position with { Y = position.Y + 0.1f };
        }

        if (_mesh?.MaterialOverride is StandardMaterial3D material)
        {
            Color color = state == BombState.Planted ? ArmedColor : CarriedColor;
            material.AlbedoColor = color;
            material.Emission = color;
        }

        if (_light is not null)
        {
            _light.LightEnergy = state == BombState.Planted ? 1.4f : 0f;
        }

        if (state == BombState.Planted && previous != BombState.Planted)
        {
            _timeSincePlanted = 0f;
            _beepCooldown = 0f;
            _audio?.PlayBombPlanted(position);
        }

        if (state == BombState.Defused && previous != BombState.Defused)
        {
            _audio?.PlayBombDefused(position);
        }

        if (state == BombState.Exploded && previous != BombState.Exploded)
        {
            _audio?.PlayBombExplosion(position);
        }
    }

    public override void _Process(double delta)
    {
        if (_state != BombState.Planted)
        {
            return;
        }

        var step = (float)delta;
        _timeSincePlanted += step;
        _beepCooldown -= step;

        if (_beepCooldown > 0f)
        {
            return;
        }

        _audio?.PlayBombBeep(GlobalPosition);
        _beepCooldown = BeepInterval(_timeSincePlanted);

        // Partlayışa yaxın pulsasiya da sürətlənir.
        if (_light is not null)
        {
            _light.LightEnergy = _light.LightEnergy > 1f ? 0.5f : 2.2f;
        }
    }

    /// <summary>
    /// Partlayışa qalan vaxta görə beep intervalı; sona doğru sürətlənir.
    /// Taymer müddəti keçilsə də interval minimumdan aşağı düşmür.
    /// </summary>
    private static float BeepInterval(float timeSincePlanted)
    {
        float progress = Mathf.Clamp(timeSincePlanted / GameConstants.BombTimerSeconds, 0f, 1f);
        return Mathf.Lerp(SlowBeepSeconds, FastBeepSeconds, progress * progress);
    }
}
