using Godot;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Client.UI;

/// <summary>
/// PRD 78, 79, 152 - Prototype HUD: can, patron, ping və sadə crosshair.
///
/// Tam HUD (pul, round taymer, kill feed, minimap) Phase 2/3-də gələcək.
/// Competitive HUD mümkün qədər təmiz olmalıdır — bura yalnız lazım olanı qoyuruq.
/// </summary>
public sealed partial class PrototypeHud : Control
{
    private Label? _health;
    private Label? _ping;
    private Label? _status;

    public override void _Ready()
    {
        // AnchorsAndOffsets: yalniz anchor teyin etmek kifayet etmir,
        // offset-ler de sifirlanmalidir ki, Control hequqeten tam ekrani tutsun.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _health = CreateLabel(new Vector2(32f, -72f), LayoutPreset.BottomLeft, 28);
        _ping = CreateLabel(new Vector2(-140f, 24f), LayoutPreset.TopRight, 16);
        _status = CreateLabel(new Vector2(32f, 24f), LayoutPreset.TopLeft, 16);

        AddChild(new Crosshair { Name = "Crosshair" });
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

/// <summary>PRD 79 - Crosshair. Prototype-da statik; tam customization Phase 2-də.</summary>
public sealed partial class Crosshair : Control
{
    private const float Gap = 4f;
    private const float Length = 7f;
    private const float Thickness = 2f;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        GetWindow().SizeChanged += QueueRedraw;
    }

    public override void _Draw()
    {
        Vector2 center = GetViewportRect().Size / 2f;
        Color color = Colors.LimeGreen;

        // Yuxarı / aşağı / sol / sağ xətlər.
        DrawRect(new Rect2(center.X - (Thickness / 2f), center.Y - Gap - Length, Thickness, Length), color);
        DrawRect(new Rect2(center.X - (Thickness / 2f), center.Y + Gap, Thickness, Length), color);
        DrawRect(new Rect2(center.X - Gap - Length, center.Y - (Thickness / 2f), Length, Thickness), color);
        DrawRect(new Rect2(center.X + Gap, center.Y - (Thickness / 2f), Length, Thickness), color);
    }
}
