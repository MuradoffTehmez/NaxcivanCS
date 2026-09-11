using NaxcivanCS.Shared.AntiCheat;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 124 - Match state ve rank/anti-cheat testleri.</summary>
public sealed class MatchRulesTests
{
    [Fact]
    public void ThirteenRounds_WinsTheMatch()
        => Assert.Equal(Team.Alpha, MatchRules.GetWinner(13, 7));

    [Fact]
    public void TwelveAll_IsNotAWin()
        => Assert.Equal(Team.None, MatchRules.GetWinner(12, 12));

    [Fact]
    public void TwelveAll_TriggersOvertime()
        => Assert.True(MatchRules.RequiresOvertime(12, 12));

    [Fact]
    public void SideSwap_HappensAfterTwelfthRound()
    {
        Assert.True(MatchRules.IsSideSwapRound(12));
        Assert.False(MatchRules.IsSideSwapRound(11));
    }

    [Fact]
    public void PhaseDurations_MatchPrdDefaults()
    {
        Assert.Equal(10f, MatchRules.PhaseDuration(RoundPhase.FreezeTime));
        Assert.Equal(20f, MatchRules.PhaseDuration(RoundPhase.BuyTime));
        Assert.Equal(105f, MatchRules.PhaseDuration(RoundPhase.Active));
        Assert.Equal(40f, MatchRules.PhaseDuration(RoundPhase.BombPlanted));
    }
}

public sealed class SuspicionRulesTests
{
    [Fact]
    public void TamperedClient_IsTheHeaviestViolation()
        => Assert.Equal(40, SuspicionRules.Weight(Violation.TamperedClient));

    [Fact]
    public void EightyPoints_TriggersReview()
        => Assert.Equal(SuspicionAction.Review, SuspicionRules.Evaluate(80));

    [Fact]
    public void HundredPoints_TriggersTemporaryRestriction()
        => Assert.Equal(SuspicionAction.TemporaryRestriction, SuspicionRules.Evaluate(100));

    [Fact]
    public void LowScore_TriggersNothing()
        => Assert.Equal(SuspicionAction.None, SuspicionRules.Evaluate(35));

    [Fact]
    public void Apply_AccumulatesScore()
    {
        int score = SuspicionRules.Apply(0, Violation.ImpossibleFireRate);
        score = SuspicionRules.Apply(score, Violation.TamperedClient);
        Assert.Equal(70, score);
    }

    [Fact]
    public void FireRateValidator_RejectsTooFastShots()
    {
        // 600 RPM -> 0.1s interval
        Assert.True(ServerValidators.IsImpossibleFireRate(0.04, 0.1f));
        Assert.False(ServerValidators.IsImpossibleFireRate(0.11, 0.1f));
    }

    [Fact]
    public void MovementValidator_RejectsTeleport()
    {
        Assert.True(ServerValidators.IsImpossibleMovement(distanceMoved: 12f, maxSpeed: 6f, deltaSeconds: 0.0156f));
        Assert.False(ServerValidators.IsImpossibleMovement(distanceMoved: 0.09f, maxSpeed: 6f, deltaSeconds: 0.0156f));
    }
}
