using Godot;

namespace NaxcivanCS.Client.UI;

/// <summary>
/// PRD 78, 79, 152 - Prototype HUD: can, zireh, patron, ping və crosshair.
///
/// Tam HUD (pul, round taymer, kill feed, minimap) Phase 2/3-də gələcək.
/// Competitive HUD mümkün qədər təmiz olmalıdır — bura yalnız lazım olanı qoyulur.
/// </summary>
public sealed partial class PrototypeHud : Control
{
    private Label? _health;
    private Label? _ammo;
    private Label? _ping;
    private Label? _status;
    private Crosshair? _crosshair;

    public override void _Ready()
    {
        // AnchorsAndOffsets: yalnız anchor təyin etmək kifayət etmir,
        // offset-lər də sıfırlanmalıdır ki, Control həqiqətən tam ekranı tutsun.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _health = CreateLabel(new Vector2(32f, -72f), LayoutPreset.BottomLeft, 28);
        _ammo = CreateLabel(new Vector2(-150f, -72f), LayoutPreset.BottomRight, 28);
        _ping = CreateLabel(new Vector2(-140f, 24f), LayoutPreset.TopRight, 16);
        _status = CreateLabel(new Vector2(32f, 24f), LayoutPreset.TopLeft, 16);

        _crosshair = new Crosshair { Name = "Crosshair" };
        AddChild(_crosshair);
    }

    public void UpdateHealth(int health, int armor)
    {
        if (_health is not null)
        {
            _health.Text = $"{health}  ♦ {armor}";
            _health.Modulate = health switch
            {
                <= 25 => Colors.OrangeRed,
                <= 50 => Colors.Orange,
                _ => Colors.White,
            };
        }
    }

    /// <summary>PRD 78 - Şarjor / ehtiyat patron.</summary>
    public void UpdateAmmo(int magazine, int reserve, bool reloading)
    {
        if (_ammo is null)
        {
            return;
        }

        _ammo.Text = reloading ? "RELOAD" : $"{magazine} / {reserve}";
        _ammo.Modulate = reloading || magazine == 0
            ? Colors.OrangeRed
            : magazine <= 5 ? Colors.Orange : Colors.White;
    }

    public void UpdatePing(double pingMs)
    {
        if (_ping is not null)
        {
            _ping.Text = $"{pingMs:F0} ms";
            _ping.Modulate = pingMs switch
            {
                <= 40 => Colors.LightGreen,
                <= 90 => Colors.White,
                _ => Colors.OrangeRed,
            };
        }
    }

    public void UpdateStatus(string text)
    {
        if (_status is not null)
        {
            _status.Text = text;
        }
    }

    /// <summary>PRD 78 - Hit feedback: vurulma təsdiqi.</summary>
    public void ShowHitMarker(bool killed) => _crosshair?.Hit(killed);

    /// <summary>PRD 79 - Crosshair recoil artdıqca açılır.</summary>
    public void SetCrosshairSpread(float normalized) => _crosshair?.SetSpread(normalized);

    private Label CreateLabel(Vector2 offset, LayoutPreset preset, int fontSize)
    {
        var label = new Label { Text = string.Empty };
        label.SetAnchorsAndOffsetsPreset(preset);
        label.Position += offset;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        AddChild(label);
        return label;
    }
}

/// <summary>
/// PRD 79 - Crosshair.
///
/// Dinamikdir: recoil artdıqca açılır, vurulanda hit marker göstərir.
/// Tam customization (ölçü, qalınlıq, rəng, static/dynamic) Phase 2-dədir.
/// </summary>
public sealed partial class Crosshair : Control
{
    private const float BaseGap = 4f;
    private const float MaxExtraGap = 22f;
    private const float Length = 7f;
    private const float Thickness = 2f;
    private const float HitMarkerSeconds = 0.14f;

    private float _spread;
    private float _hitRemaining;
    private bool _hitWasKill;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        GetWindow().SizeChanged += QueueRedraw;
    }

    public void SetSpread(float normalized)
    {
        float clamped = Mathf.Clamp(normalized, 0f, 1f);
        if (Mathf.Abs(clamped - _spread) > 0.005f)
        {
            _spread = clamped;
            QueueRedraw();
        }
    }

    public void Hit(bool killed)
    {
        _hitRemaining = HitMarkerSeconds;
        _hitWasKill = killed;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_hitRemaining <= 0f)
        {
            return;
        }

        _hitRemaining -= (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = GetViewportRect().Size / 2f;
        Color color = Colors.LimeGreen;
        float gap = BaseGap + (MaxExtraGap * _spread);

        DrawRect(new Rect2(center.X - (Thickness / 2f), center.Y - gap - Length, Thickness, Length), color);
        DrawRect(new Rect2(center.X - (Thickness / 2f), center.Y + gap, Thickness, Length), color);
        DrawRect(new Rect2(center.X - gap - Length, center.Y - (Thickness / 2f), Length, Thickness), color);
        DrawRect(new Rect2(center.X + gap, center.Y - (Thickness / 2f), Length, Thickness), color);

        if (_hitRemaining <= 0f)
        {
            return;
        }

        // Hit marker: diaqonal X. Kill olduqda qırmızı.
        Color hitColor = _hitWasKill ? new Color("ff4d3d") : Colors.White;
        hitColor.A = Mathf.Clamp(_hitRemaining / HitMarkerSeconds, 0f, 1f);

        const float inner = 5f;
        const float outer = 12f;

        DrawLine(center + new Vector2(inner, inner), center + new Vector2(outer, outer), hitColor, 2f);
        DrawLine(center + new Vector2(-inner, inner), center + new Vector2(-outer, outer), hitColor, 2f);
        DrawLine(center + new Vector2(inner, -inner), center + new Vector2(outer, -outer), hitColor, 2f);
        DrawLine(center + new Vector2(-inner, -inner), center + new Vector2(-outer, -outer), hitColor, 2f);
    }
}
