// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 124 - Economy unit testleri.</summary>
public sealed class EconomyRulesTests
{
    [Theory]
    [InlineData(1, 1400)]
    [InlineData(2, 1900)]
    [InlineData(3, 2400)]
    [InlineData(4, 2900)]
    [InlineData(5, 3400)]
    [InlineData(9, 3400)] // PRD 26: 5+ hemise 3400-de qalir
    public void LossBonus_FollowsPrdLadder(int consecutiveLosses, int expected)
        => Assert.Equal(expected, EconomyRules.LossBonus(consecutiveLosses));

    [Fact]
    public void LossBonus_IsZeroWhenNoLossStreak()
        => Assert.Equal(0, EconomyRules.LossBonus(0));

    [Fact]
    public void RoundEndReward_WinnerGetsFixedReward()
        => Assert.Equal(GameConstants.RoundWinReward, EconomyRules.RoundEndReward(won: true, consecutiveLossesIfLost: 3));

    [Fact]
    public void AddMoney_ClampsToMaximum()
        => Assert.Equal(GameConstants.MaxMoney, EconomyRules.AddMoney(GameConstants.MaxMoney - 100, 5000));

    [Fact]
    public void AddMoney_NeverGoesNegative()
        => Assert.Equal(0, EconomyRules.AddMoney(200, -500));

    [Fact]
    public void TryPurchase_SucceedsWhenAffordable()
    {
        Assert.True(EconomyRules.TryPurchase(2700, 2700, out int remaining));
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void TryPurchase_RejectsWhenTooExpensive()
    {
        Assert.False(EconomyRules.TryPurchase(800, 2700, out int remaining));
        Assert.Equal(800, remaining);
    }

    [Fact]
    public void StartMoney_MatchesPrd()
        => Assert.Equal(800, GameConstants.StartMoney);
}
