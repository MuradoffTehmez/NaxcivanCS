// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.UI;

/// <summary>
/// PRD 128 - Tab ilə açılan scoreboard.
///
/// Sətirlər serverdən gəlir; client heç bir statistikanı özü hesablamır
/// (PRD 156). Ölü oyunçular sönük göstərilir.
/// </summary>
public sealed partial class Scoreboard : Control
{
    private static readonly Color AlphaColor = new("e0623c");
    private static readonly Color BravoColor = new("3f9ad6");

    private IReadOnlyList<PacketCodec.ScoreboardEntry> _entries = Array.Empty<PacketCodec.ScoreboardEntry>();
    private VBoxContainer? _rows;
    private Label? _header;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;

        var panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(520f, 0f) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.07f, 0.07f, 0.88f),
            BorderColor = new Color(0.35f, 0.33f, 0.3f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 14,
            ContentMarginBottom = 14,
        };
        style.SetBorderWidthAll(1);
        panel.AddThemeStyleboxOverride("panel", style);

        // Paneli ekranın mərkəzinə oturt.
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        panel.Position = new Vector2(-260f, -170f);
        AddChild(panel);

        var column = new VBoxContainer();
        panel.AddChild(column);

        _header = new Label { Text = "Oyunçu                    K     D    Pul" };
        _header.AddThemeFontSizeOverride("font_size", 15);
        _header.Modulate = new Color("9c968c");
        column.AddChild(_header);

        column.AddChild(new HSeparator());

        _rows = new VBoxContainer();
        column.AddChild(_rows);
    }

    public void SetEntries(IReadOnlyList<PacketCodec.ScoreboardEntry> entries)
    {
        _entries = entries;

        if (Visible)
        {
            Rebuild();
        }
    }

    public void SetShown(bool shown)
    {
        if (shown == Visible)
        {
            return;
        }

        Visible = shown;

        if (shown)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        if (_rows is null)
        {
            return;
        }

        foreach (Node child in _rows.GetChildren())
        {
            child.QueueFree();
        }

        foreach (PacketCodec.ScoreboardEntry entry in _entries)
        {
            string name = entry.Username.Length > 18 ? entry.Username[..18] : entry.Username;

            var row = new Label
            {
                Text = $"{name,-20} {entry.Kills,3} {entry.Deaths,5} {entry.Money,7}",
                Modulate = RowColor(entry),
            };

            row.AddThemeFontSizeOverride("font_size", 16);
            _rows.AddChild(row);
        }

        if (_entries.Count == 0)
        {
            _rows.AddChild(new Label { Text = "Oyunçu yoxdur", Modulate = new Color("7a746b") });
        }
    }

    /// <summary>Komanda rəngi; ölü oyunçu sönük göstərilir.</summary>
    private static Color RowColor(PacketCodec.ScoreboardEntry entry)
    {
        Color color = entry.Team == Team.Alpha ? AlphaColor : BravoColor;
        return entry.IsAlive ? color : color.Darkened(0.45f);
    }
}
