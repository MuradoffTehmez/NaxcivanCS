using Godot;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Client.UI;

/// <summary>
/// PRD 10, 78 - Round paneli: taymer, komanda skorları və faza banneri.
///
/// <para>
/// Competitive HUD təmiz olmalıdır (PRD 78), ona görə yuxarıda yalnız üç
/// rəqəm var: Alpha skoru, qalan vaxt, Bravo skoru. Faza dəyişəndə qısa
/// mərkəzi banner çıxır və sönür — oyunçu nə baş verdiyini bilməlidir,
/// amma ekran daim yazı ilə dolu olmamalıdır.
/// </para>
/// </summary>
public sealed partial class RoundHud : Control
{
    private const float BannerSeconds = 2.6f;

    private static readonly Color AlphaColor = new("e0623c");
    private static readonly Color BravoColor = new("3f9ad6");

    private Label? _timer;
    private Label? _alphaScore;
    private Label? _bravoScore;
    private Label? _banner;
    private Label? _phase;

    private float _bannerRemaining;
    private RoundPhase _lastPhase = RoundPhase.Warmup;
    private int _lastRoundNumber;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _alphaScore = CreateLabel(new Vector2(-96f, 16f), LayoutPreset.CenterTop, 30, AlphaColor);
        _timer = CreateLabel(new Vector2(-32f, 16f), LayoutPreset.CenterTop, 30, Colors.White);
        _bravoScore = CreateLabel(new Vector2(48f, 16f), LayoutPreset.CenterTop, 30, BravoColor);
        _phase = CreateLabel(new Vector2(-48f, 56f), LayoutPreset.CenterTop, 14, new Color("bdb6ab"));

        _banner = CreateLabel(new Vector2(-220f, -90f), LayoutPreset.Center, 26, Colors.White);
        _banner.Size = new Vector2(440f, 40f);
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
    }

    public void UpdateRound(
        RoundPhase phase,
        MatchState matchState,
        float timeRemaining,
        int roundNumber,
        int alphaScore,
        int bravoScore,
        Team roundWinner,
        RoundEndReason endReason)
    {
        if (_timer is not null)
        {
            var minutes = (int)(timeRemaining / 60f);
            var seconds = (int)(timeRemaining % 60f);
            _timer.Text = $"{minutes}:{seconds:00}";

            // Son 10 saniyə qırmızı — vaxt təzyiqi görünməlidir.
            _timer.Modulate = phase == RoundPhase.Active && timeRemaining <= 10f
                ? Colors.OrangeRed
                : Colors.White;
        }

        if (_alphaScore is not null)
        {
            _alphaScore.Text = alphaScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (_bravoScore is not null)
        {
            _bravoScore.Text = bravoScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (_phase is not null)
        {
            _phase.Text = matchState == MatchState.Overtime
                ? $"OT · Round {roundNumber}"
                : $"Round {roundNumber}";
        }

        if (phase != _lastPhase || roundNumber != _lastRoundNumber)
        {
            ShowBannerFor(phase, matchState, roundWinner, endReason, roundNumber);
            _lastPhase = phase;
            _lastRoundNumber = roundNumber;
        }
    }

    public override void _Process(double delta)
    {
        if (_banner is null || _bannerRemaining <= 0f)
        {
            return;
        }

        _bannerRemaining -= (float)delta;

        // Son yarım saniyədə sönür.
        float alpha = Mathf.Clamp(_bannerRemaining / 0.5f, 0f, 1f);
        Color color = _banner.Modulate;
        color.A = alpha;
        _banner.Modulate = color;

        if (_bannerRemaining <= 0f)
        {
            _banner.Text = string.Empty;
        }
    }

    private void ShowBannerFor(
        RoundPhase phase, MatchState matchState, Team winner, RoundEndReason reason, int roundNumber)
    {
        (string text, Color color) = phase switch
        {
            RoundPhase.FreezeTime => ($"Round {roundNumber}", Colors.White),
            RoundPhase.BuyTime => ("Alış vaxtı", new Color("d98b45")),
            RoundPhase.Active => ("Başla!", new Color("8fd14f")),
            RoundPhase.BombPlanted => ("Bomba yerləşdirildi", Colors.OrangeRed),
            RoundPhase.RoundEnd => RoundEndText(winner, reason),
            _ => (string.Empty, Colors.White),
        };

        if (matchState == MatchState.Finished)
        {
            text = winner == Team.None ? "Match bitdi" : $"{TeamName(winner)} matçı qazandı!";
            color = TeamColor(winner);
        }

        if (string.IsNullOrEmpty(text) || _banner is null)
        {
            return;
        }

        _banner.Text = text;
        _banner.Modulate = color;
        _bannerRemaining = BannerSeconds;
    }

    private static (string Text, Color Color) RoundEndText(Team winner, RoundEndReason reason)
    {
        string why = reason switch
        {
            RoundEndReason.AlphaEliminated or RoundEndReason.BravoEliminated => "komanda məhv edildi",
            RoundEndReason.TimeExpired => "vaxt bitdi",
            RoundEndReason.BombExploded => "bomba partladı",
            RoundEndReason.BombDefused => "bomba zərərsizləşdirildi",
            _ => string.Empty,
        };

        string text = winner == Team.None
            ? "Round bitdi"
            : $"{TeamName(winner)} qazandı — {why}";

        return (text, TeamColor(winner));
    }

    private static string TeamName(Team team) => team switch
    {
        Team.Alpha => "Alpha",
        Team.Bravo => "Bravo",
        _ => "—",
    };

    private static Color TeamColor(Team team) => team switch
    {
        Team.Alpha => AlphaColor,
        Team.Bravo => BravoColor,
        _ => Colors.White,
    };

    private Label CreateLabel(Vector2 offset, LayoutPreset preset, int fontSize, Color color)
    {
        var label = new Label { Text = string.Empty, Modulate = color };
        label.SetAnchorsAndOffsetsPreset(preset);
        label.Position += offset;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 5);
        AddChild(label);
        return label;
    }
}
