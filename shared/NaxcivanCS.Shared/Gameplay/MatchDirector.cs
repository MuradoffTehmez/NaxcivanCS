// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Bir tick-də match-də baş verən ən əhəmiyyətli hadisə.</summary>
public enum MatchEvent
{
    None = 0,

    /// <summary>Faza dəyişdi (məs. FreezeTime → BuyTime).</summary>
    PhaseChanged = 1,

    /// <summary>Round bitdi; qalib və səbəb müvafiq sahələrdədir.</summary>
    RoundEnded = 2,

    /// <summary>Yeni round başladı — oyunçular respawn olunmalıdır.</summary>
    RoundStarted = 3,

    /// <summary>PRD 9 - Yarı vaxt: komandalar tərəf dəyişir.</summary>
    HalftimeSwap = 4,

    /// <summary>Match bitdi.</summary>
    MatchEnded = 5,
}

/// <summary>Round sonunda komandaya veriləcək pul (PRD 25, 26).</summary>
public readonly record struct RoundPayout(int AlphaReward, int BravoReward);

/// <summary>
/// PRD 9, 10, 128 - Match və round idarəçisi.
///
/// <para>
/// Saf state machine: Godot-a, şəbəkəyə və ya oyunçu obyektlərinə istinad
/// etmir. Girişi yalnız "neçə saniyə keçdi" və "hər komandada neçə nəfər
/// sağdır"-dır. Bu, bütün round məntiqinin unit testlə yoxlanmasına imkan
/// verir — round keçidləri FPS-də ən çox baq çıxan yerlərdəndir (PRD 137/7:
/// "round reset problemsizdir").
/// </para>
///
/// <para>
/// Faza axını (PRD 10):
/// <c>Warmup → FreezeTime → BuyTime → Active → RoundEnd → FreezeTime → ...</c>
/// </para>
/// </summary>
public sealed class MatchDirector
{
    /// <summary>Round başlaması üçün lazım olan minimum oyunçu sayı.</summary>
    private readonly int _minimumPlayers;

    private int _alphaAtRoundStart;
    private int _bravoAtRoundStart;
    private int _alphaLossStreak;
    private int _bravoLossStreak;

    private readonly RoundTimings _timings;

    public MatchDirector(int minimumPlayers = 1, RoundTimings? timings = null)
    {
        _minimumPlayers = Math.Max(1, minimumPlayers);
        _timings = timings ?? RoundTimings.Defaults;
    }

    public MatchState State { get; private set; } = MatchState.WaitingForPlayers;

    public RoundPhase Phase { get; private set; } = RoundPhase.Warmup;

    public float PhaseTimeRemaining { get; private set; }

    /// <summary>1-dən başlayır; Warmup zamanı 0-dır.</summary>
    public int RoundNumber { get; private set; }

    public int AlphaScore { get; private set; }

    public int BravoScore { get; private set; }

    public Team RoundWinner { get; private set; } = Team.None;

    public RoundEndReason LastRoundEndReason { get; private set; } = RoundEndReason.None;

    public Team MatchWinner { get; private set; } = Team.None;

    /// <summary>Sonuncu round üçün hesablanmış pul (PRD 25, 26).</summary>
    public RoundPayout LastPayout { get; private set; }

    /// <summary>PRD 10 - Freeze time-da oyunçu tərpənə bilməz.</summary>
    public bool MovementLocked => Phase == RoundPhase.FreezeTime;

    /// <summary>Atəş yalnız aktiv roundda mümkündür.</summary>
    public bool CombatEnabled => Phase is RoundPhase.Active or RoundPhase.BombPlanted;

    /// <summary>PRD 27 - Alış yalnız buy fazasında mümkündür.</summary>
    public bool BuyEnabled => Phase is RoundPhase.FreezeTime or RoundPhase.BuyTime;

    /// <summary>
    /// Bir server tick-i.
    /// </summary>
    /// <param name="delta">Tick müddəti, saniyə.</param>
    /// <param name="aliveAlpha">Sağ qalan Alpha oyunçuları.</param>
    /// <param name="aliveBravo">Sağ qalan Bravo oyunçuları.</param>
    /// <param name="connectedPlayers">Serverə qoşulu ümumi oyunçu sayı.</param>
    public MatchEvent Tick(float delta, int aliveAlpha, int aliveBravo, int connectedPlayers)
    {
        if (State == MatchState.Finished)
        {
            return MatchEvent.None;
        }

        if (State == MatchState.WaitingForPlayers)
        {
            return connectedPlayers >= _minimumPlayers ? BeginRound() : MatchEvent.None;
        }

        PhaseTimeRemaining -= delta;

        // Aktiv round vaxtından əvvəl də bitə bilər — komandanın tamamı ölürsə.
        if (Phase == RoundPhase.Active && TryResolveElimination(aliveAlpha, aliveBravo) is { } eliminated)
        {
            return eliminated;
        }

        // PRD 8 - Bomba yerləşdirildikdən sonra yalnız MÜDAFIƏNIN məhv edilməsi
        // roundu bitirir: defuse edə biləcək heç kim qalmayıb, ona görə taymeri
        // sona qədər gözlətmək mənasızdır.
        //
        // Əks hal qəsdən fərqlidir: hücum edənlərin hamısı ölsə də round davam
        // edir, çünki yerləşdirilmiş bomba hələ partlaya bilər.
        if (Phase == RoundPhase.BombPlanted && _bravoAtRoundStart > 0 && aliveBravo == 0)
        {
            return EndRound(Team.Alpha, RoundEndReason.BravoEliminated);
        }

        if (PhaseTimeRemaining > 0f)
        {
            return MatchEvent.None;
        }

        return AdvancePhase();
    }

    /// <summary>
    /// PRD 8 - Komandanın tamamı öldürsə round dərhal bitir.
    ///
    /// Round başlayanda komandada heç kim yoxdursa, o komanda "məhv edilmiş"
    /// sayılmır — əks halda tək oyunçu ilə test edərkən round sonsuz döngəyə düşür.
    /// </summary>
    private MatchEvent? TryResolveElimination(int aliveAlpha, int aliveBravo)
    {
        if (_alphaAtRoundStart > 0 && aliveAlpha == 0)
        {
            return EndRound(Team.Bravo, RoundEndReason.AlphaEliminated);
        }

        if (_bravoAtRoundStart > 0 && aliveBravo == 0)
        {
            return EndRound(Team.Alpha, RoundEndReason.BravoEliminated);
        }

        return null;
    }

    private MatchEvent AdvancePhase() => Phase switch
    {
        RoundPhase.FreezeTime => EnterPhase(RoundPhase.BuyTime),
        RoundPhase.BuyTime => EnterPhase(RoundPhase.Active),

        // PRD 8 - Vaxt bitdi: hücum edənlər bombanı yerləşdirə bilmədi,
        // müdafiə qazanır.
        RoundPhase.Active => EndRound(Team.Bravo, RoundEndReason.TimeExpired),

        // Bomba partladı (Phase 3-də bomba sistemi ilə tamamlanacaq).
        RoundPhase.BombPlanted => EndRound(Team.Alpha, RoundEndReason.BombExploded),

        RoundPhase.RoundEnd => StartNextRoundOrFinish(),
        _ => MatchEvent.None,
    };

    private MatchEvent StartNextRoundOrFinish()
    {
        if (MatchRules.IsMatchOver(AlphaScore, BravoScore))
        {
            MatchWinner = MatchRules.GetWinner(AlphaScore, BravoScore);
            State = MatchState.Finished;
            Phase = RoundPhase.RoundEnd;
            PhaseTimeRemaining = 0f;
            return MatchEvent.MatchEnded;
        }

        // PRD 9 - 12 round tamamlandıqda tərəflər dəyişir.
        if (MatchRules.IsSideSwapRound(AlphaScore + BravoScore))
        {
            SwapSides();
            BeginRound();
            return MatchEvent.HalftimeSwap;
        }

        // PRD 9 - 12:12 olduqda overtime.
        //
        // Hazırda yalnız vəziyyət qeyd olunur və oyun "ilk 13" formatında davam
        // edir. PRD-dəki tam format (3 round hücum / 3 round müdafiə, ayrıca
        // başlanğıc pul) Phase 3-də round/economy sistemi tamamlananda gələcək.
        if (MatchRules.RequiresOvertime(AlphaScore, BravoScore))
        {
            State = MatchState.Overtime;
        }

        return BeginRound();
    }

    /// <summary>PRD 9 - Yarı vaxtda skorlar da komandalarla birlikdə yer dəyişir.</summary>
    private void SwapSides()
    {
        (AlphaScore, BravoScore) = (BravoScore, AlphaScore);
        (_alphaLossStreak, _bravoLossStreak) = (_bravoLossStreak, _alphaLossStreak);
        State = MatchState.Live;
    }

    private MatchEvent BeginRound()
    {
        RoundNumber++;
        RoundWinner = Team.None;
        LastRoundEndReason = RoundEndReason.None;
        if (State != MatchState.Overtime)
        {
            State = MatchState.Live;
        }

        EnterPhase(RoundPhase.FreezeTime);
        return MatchEvent.RoundStarted;
    }

    /// <summary>
    /// Round başlayanda komanda ölçülərini qeyd edir — elimination yoxlaması
    /// buna əsaslanır.
    /// </summary>
    public void RegisterRoundRoster(int alphaPlayers, int bravoPlayers)
    {
        _alphaAtRoundStart = alphaPlayers;
        _bravoAtRoundStart = bravoPlayers;
    }

    private MatchEvent EndRound(Team winner, RoundEndReason reason)
    {
        RoundWinner = winner;
        LastRoundEndReason = reason;

        if (winner == Team.Alpha)
        {
            AlphaScore++;
            _alphaLossStreak = 0;
            _bravoLossStreak++;
        }
        else if (winner == Team.Bravo)
        {
            BravoScore++;
            _bravoLossStreak = 0;
            _alphaLossStreak++;
        }

        // PRD 25, 26 - Qalib sabit mükafat, uduzan ardıcıl uduzma bonusu alır.
        LastPayout = new RoundPayout(
            winner == Team.Alpha
                ? GameConstants.RoundWinReward
                : EconomyRules.LossBonus(_alphaLossStreak),
            winner == Team.Bravo
                ? GameConstants.RoundWinReward
                : EconomyRules.LossBonus(_bravoLossStreak));

        EnterPhase(RoundPhase.RoundEnd);
        return MatchEvent.RoundEnded;
    }

    private MatchEvent EnterPhase(RoundPhase phase)
    {
        Phase = phase;
        PhaseTimeRemaining = MatchRules.PhaseDuration(phase, _timings);
        return MatchEvent.PhaseChanged;
    }

    /// <summary>Bomba yerləşdirildi (PRD 8) — round taymeri bomba taymeri ilə əvəz olunur.</summary>
    public MatchEvent OnBombPlanted()
    {
        if (Phase != RoundPhase.Active)
        {
            return MatchEvent.None;
        }

        return EnterPhase(RoundPhase.BombPlanted);
    }

    /// <summary>Bomba zərərsizləşdirildi (PRD 8).</summary>
    public MatchEvent OnBombDefused()
    {
        if (Phase != RoundPhase.BombPlanted)
        {
            return MatchEvent.None;
        }

        return EndRound(Team.Bravo, RoundEndReason.BombDefused);
    }
}
