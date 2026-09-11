// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>
/// PRD 9, 10, 128 - Round ve match state machine.
///
/// PRD 137/7 "round reset problemsizdir" - round kecidleri FPS-de en cox baq
/// cixan yerlerdendir, ona gore burada genis yoxlanilir.
/// </summary>
public sealed class MatchDirectorTests
{
    private const float Tick = GameConstants.ServerTickIntervalSeconds;

    /// <summary>PRD 26 - Ilk uc ardicil uduzma bonusu.</summary>
    private static readonly int[] ExpectedLossLadder = { 1400, 1900, 2400 };

    /// <summary>5v5 dolu serverle direktoru canlandirir ve ilk roundu baslayir.</summary>
    private static MatchDirector StartedMatch(int alpha = 5, int bravo = 5)
    {
        var director = new MatchDirector();
        director.Tick(Tick, alpha, bravo, connectedPlayers: alpha + bravo);
        director.RegisterRoundRoster(alpha, bravo);
        return director;
    }

    /// <summary>
    /// Verilen muddet qeder tick edir.
    ///
    /// <paramref name="stopOn"/> verilibse hemin hadisede derhal dayanir;
    /// eks halda butun mudddeti gedir ve GORULEN SON hadiseni qaytarir.
    /// (Ilk hadisede dayansaydi, cox fazali kecidler yarimciq qalardi.)
    /// </summary>
    private static MatchEvent Advance(MatchDirector director, float seconds,
        int aliveAlpha = 5, int aliveBravo = 5, MatchEvent stopOn = MatchEvent.None)
    {
        int ticks = (int)(seconds / Tick);
        MatchEvent last = MatchEvent.None;

        for (int i = 0; i < ticks; i++)
        {
            MatchEvent result = director.Tick(Tick, aliveAlpha, aliveBravo, connectedPlayers: 10);

            if (result == MatchEvent.None)
            {
                continue;
            }

            last = result;

            if (stopOn != MatchEvent.None && result == stopOn)
            {
                return result;
            }
        }

        return last;
    }

    [Fact]
    public void WaitsForPlayers_BeforeStartingAnyRound()
    {
        var director = new MatchDirector(minimumPlayers: 2);

        Assert.Equal(MatchEvent.None, director.Tick(Tick, 0, 0, connectedPlayers: 0));
        Assert.Equal(MatchState.WaitingForPlayers, director.State);
        Assert.Equal(0, director.RoundNumber);

        Assert.Equal(MatchEvent.RoundStarted, director.Tick(Tick, 0, 0, connectedPlayers: 2));
        Assert.Equal(1, director.RoundNumber);
    }

    [Fact]
    public void FirstRound_StartsInFreezeTime()
    {
        MatchDirector director = StartedMatch();

        Assert.Equal(RoundPhase.FreezeTime, director.Phase);
        Assert.Equal(GameConstants.FreezeTimeSeconds, director.PhaseTimeRemaining, 1);
    }

    [Fact]
    public void PhaseOrder_FollowsPrd()
    {
        // PRD 10: Freeze -> Buy -> Active
        MatchDirector director = StartedMatch();

        Advance(director, GameConstants.FreezeTimeSeconds + 0.1f);
        Assert.Equal(RoundPhase.BuyTime, director.Phase);

        Advance(director, GameConstants.BuyTimeSeconds + 0.1f);
        Assert.Equal(RoundPhase.Active, director.Phase);
    }

    [Fact]
    public void MovementIsLocked_OnlyDuringFreezeTime()
    {
        // PRD 10 - Freeze time-da oyuncu terpene bilmez.
        MatchDirector director = StartedMatch();
        Assert.True(director.MovementLocked);

        Advance(director, GameConstants.FreezeTimeSeconds + 0.1f);
        Assert.False(director.MovementLocked);
    }

    [Fact]
    public void CombatIsDisabled_UntilActivePhase()
    {
        MatchDirector director = StartedMatch();
        Assert.False(director.CombatEnabled);

        Advance(director, GameConstants.FreezeTimeSeconds + 0.1f);
        Assert.False(director.CombatEnabled); // buy time

        Advance(director, GameConstants.BuyTimeSeconds + 0.1f);
        Assert.True(director.CombatEnabled);
    }

    [Fact]
    public void BuyIsEnabled_DuringFreezeAndBuyPhases()
    {
        // PRD 27 - Alis yalniz muveqqeti pencerede mumkundur.
        MatchDirector director = StartedMatch();
        Assert.True(director.BuyEnabled);

        Advance(director, GameConstants.FreezeTimeSeconds + 0.1f);
        Assert.True(director.BuyEnabled);

        Advance(director, GameConstants.BuyTimeSeconds + 0.1f);
        Assert.False(director.BuyEnabled);
    }

    [Fact]
    public void EliminatingAlpha_GivesRoundToBravo()
    {
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        Assert.Equal(RoundPhase.Active, director.Phase);

        MatchEvent result = director.Tick(Tick, aliveAlpha: 0, aliveBravo: 3, connectedPlayers: 10);

        Assert.Equal(MatchEvent.RoundEnded, result);
        Assert.Equal(Team.Bravo, director.RoundWinner);
        Assert.Equal(RoundEndReason.AlphaEliminated, director.LastRoundEndReason);
        Assert.Equal(1, director.BravoScore);
        Assert.Equal(0, director.AlphaScore);
    }

    [Fact]
    public void RoundTimeExpiring_GivesRoundToDefenders()
    {
        // PRD 8 - Hucum edenler bombani yerlesdire bilmedise mudafie qazanir.
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);

        MatchEvent result = Advance(director, GameConstants.RoundTimeSeconds + 0.2f,
            aliveAlpha: 2, aliveBravo: 2, stopOn: MatchEvent.RoundEnded);

        Assert.Equal(MatchEvent.RoundEnded, result);
        Assert.Equal(Team.Bravo, director.RoundWinner);
        Assert.Equal(RoundEndReason.TimeExpired, director.LastRoundEndReason);
    }

    [Fact]
    public void EmptyTeamAtRoundStart_DoesNotCountAsEliminated()
    {
        // Tek oyuncu ile test ederken round sonsuz dongeye dusmemelidir.
        var director = new MatchDirector();
        director.Tick(Tick, 1, 0, connectedPlayers: 1);
        director.RegisterRoundRoster(alphaPlayers: 1, bravoPlayers: 0);

        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f,
            aliveAlpha: 1, aliveBravo: 0);

        Assert.Equal(RoundPhase.Active, director.Phase);
        Assert.Equal(0, director.AlphaScore);
        Assert.Equal(0, director.BravoScore);
    }

    [Fact]
    public void RoundEnd_LeadsToNextRound()
    {
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.Tick(Tick, 0, 3, connectedPlayers: 10);

        Assert.Equal(RoundPhase.RoundEnd, director.Phase);

        MatchEvent next = Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f,
            stopOn: MatchEvent.RoundStarted);

        Assert.Equal(MatchEvent.RoundStarted, next);
        Assert.Equal(2, director.RoundNumber);
        Assert.Equal(RoundPhase.FreezeTime, director.Phase);
    }

    [Fact]
    public void WinnerGetsFixedReward_LoserGetsLossBonus()
    {
        // PRD 25, 26
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.Tick(Tick, aliveAlpha: 0, aliveBravo: 3, connectedPlayers: 10);

        Assert.Equal(GameConstants.RoundWinReward, director.LastPayout.BravoReward);
        Assert.Equal(EconomyRules.LossBonus(1), director.LastPayout.AlphaReward);
    }

    [Fact]
    public void ConsecutiveLosses_IncreaseTheBonus()
    {
        MatchDirector director = StartedMatch();

        var payouts = new List<int>();
        for (int round = 0; round < 3; round++)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
            director.Tick(Tick, aliveAlpha: 0, aliveBravo: 3, connectedPlayers: 10);
            payouts.Add(director.LastPayout.AlphaReward);

            Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f, stopOn: MatchEvent.RoundStarted);
            director.RegisterRoundRoster(5, 5);
        }

        Assert.Equal(ExpectedLossLadder, payouts);
    }

    [Fact]
    public void WinningAfterLosses_ResetsTheLossStreak()
    {
        MatchDirector director = StartedMatch();

        // Alpha iki round uduzur.
        for (int i = 0; i < 2; i++)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
            director.Tick(Tick, 0, 3, connectedPlayers: 10);
            Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f, stopOn: MatchEvent.RoundStarted);
            director.RegisterRoundRoster(5, 5);
        }

        // Alpha qazanir.
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.Tick(Tick, 3, 0, connectedPlayers: 10);
        Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f, stopOn: MatchEvent.RoundStarted);
        director.RegisterRoundRoster(5, 5);

        // Sonra yeniden uduzur - bonus bassdan baslamalidir.
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.Tick(Tick, 0, 3, connectedPlayers: 10);

        Assert.Equal(EconomyRules.LossBonus(1), director.LastPayout.AlphaReward);
    }

    [Fact]
    public void BombPlanted_SwitchesToBombTimer()
    {
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);

        Assert.Equal(MatchEvent.PhaseChanged, director.OnBombPlanted());
        Assert.Equal(RoundPhase.BombPlanted, director.Phase);
        Assert.Equal(GameConstants.BombTimerSeconds, director.PhaseTimeRemaining, 1);
        Assert.True(director.CombatEnabled);
    }

    [Fact]
    public void BombPlanted_IsIgnoredOutsideActiveRound()
    {
        MatchDirector director = StartedMatch();
        Assert.Equal(MatchEvent.None, director.OnBombPlanted()); // freeze time
        Assert.Equal(RoundPhase.FreezeTime, director.Phase);
    }

    [Fact]
    public void DefusingBomb_GivesRoundToDefenders()
    {
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.OnBombPlanted();

        Assert.Equal(MatchEvent.RoundEnded, director.OnBombDefused());
        Assert.Equal(Team.Bravo, director.RoundWinner);
        Assert.Equal(RoundEndReason.BombDefused, director.LastRoundEndReason);
    }

    [Fact]
    public void BombExploding_GivesRoundToAttackers()
    {
        MatchDirector director = StartedMatch();
        Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
        director.OnBombPlanted();

        MatchEvent result = Advance(director, GameConstants.BombTimerSeconds + 0.2f,
            aliveAlpha: 1, aliveBravo: 1, stopOn: MatchEvent.RoundEnded);

        Assert.Equal(MatchEvent.RoundEnded, result);
        Assert.Equal(Team.Alpha, director.RoundWinner);
        Assert.Equal(RoundEndReason.BombExploded, director.LastRoundEndReason);
    }

    [Fact]
    public void ThirteenthWin_EndsTheMatch()
    {
        // PRD 9 - MR12: 13 round qazanan qalibdir.
        MatchDirector director = StartedMatch();

        // Yarim vaxtda terefler deyisdiyi ucun (skorlar da onlarla gedir)
        // "hemise Alpha terefi qazanir" senarisi 25 round cekir: 12-0, swap,
        // sonra 13 qalibiyyet daha.
        MatchEvent last = MatchEvent.None;
        for (int i = 0; i < 30 && director.State != MatchState.Finished; i++)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
            director.Tick(Tick, aliveAlpha: 3, aliveBravo: 0, connectedPlayers: 10);

            last = Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f);
            director.RegisterRoundRoster(5, 5);
        }

        Assert.Equal(MatchState.Finished, director.State);
        Assert.Equal(MatchEvent.MatchEnded, last);
        Assert.Equal(Team.Alpha, director.MatchWinner);
        Assert.Equal(GameConstants.RoundsToWin, director.AlphaScore);
    }

    [Fact]
    public void FinishedMatch_StopsTicking()
    {
        MatchDirector director = StartedMatch();

        for (int i = 0; i < 30 && director.State != MatchState.Finished; i++)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);
            director.Tick(Tick, 3, 0, connectedPlayers: 10);
            Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f);
            director.RegisterRoundRoster(5, 5);
        }

        int roundAtEnd = director.RoundNumber;
        Advance(director, 30f);

        Assert.Equal(roundAtEnd, director.RoundNumber);
        Assert.Equal(MatchEvent.None, director.Tick(Tick, 5, 5, connectedPlayers: 10));
    }

    [Fact]
    public void TwelveAll_MarksOvertime()
    {
        // PRD 9 - 12:12 olduqda overtime. Tam format (3 round hucum / 3 round
        // mudafie) hele tetbiq olunmayib; burada yalniz veziyyet yoxlanilir.
        MatchDirector director = StartedMatch();

        int guard = 0;
        while (!(director.AlphaScore == 12 && director.BravoScore == 12) && guard++ < 40)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);

            // Novbe ile qazandirib 12:12-ye catdiririq.
            bool alphaWins = director.AlphaScore <= director.BravoScore;
            director.Tick(Tick, alphaWins ? 3 : 0, alphaWins ? 0 : 3, connectedPlayers: 10);

            Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f);
            director.RegisterRoundRoster(5, 5);
        }

        Assert.Equal(12, director.AlphaScore);
        Assert.Equal(12, director.BravoScore);
        Assert.Equal(MatchState.Overtime, director.State);
        Assert.NotEqual(MatchState.Finished, director.State);
    }

    [Fact]
    public void Halftime_SwapsScoresAfterTwelveRounds()
    {
        // PRD 9 - 12 round tamamlandiqda terefler deyisir; skorlar da onlarla gedir.
        MatchDirector director = StartedMatch();

        MatchEvent lastEvent = MatchEvent.None;
        int guard = 0;

        while (director.AlphaScore + director.BravoScore < MatchRules.HalftimeAfterRound && guard++ < 40)
        {
            Advance(director, GameConstants.FreezeTimeSeconds + GameConstants.BuyTimeSeconds + 0.2f);

            // Alphanin 7, Bravonun 5 qalibiyyeti olsun ki, skorlar ferqli olsun.
            bool alphaWins = director.AlphaScore < 7;
            director.Tick(Tick, alphaWins ? 3 : 0, alphaWins ? 0 : 3, connectedPlayers: 10);

            lastEvent = Advance(director, GameConstants.RoundEndDelaySeconds + 0.2f);
            director.RegisterRoundRoster(5, 5);
        }

        Assert.Equal(MatchEvent.HalftimeSwap, lastEvent);

        // Yarim vaxtdan evvel Alpha 7, Bravo 5 idi -> swap sonrasi eksine.
        Assert.Equal(5, director.AlphaScore);
        Assert.Equal(7, director.BravoScore);
    }
}
