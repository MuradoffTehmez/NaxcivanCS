// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 8, 27 - Defuse kit alışı.</summary>
public sealed class BuyRulesTests
{
    private const int Rich = 5000;

    [Fact]
    public void DefuseKit_BoughtByDefenderInBuyPhase()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: true, buyEnabled: true,
            alreadyOwned: false, money: Rich, out int remaining);

        Assert.Equal(BuyResultCode.Purchased, code);
        Assert.Equal(Rich - GameConstants.DefuseKitPrice, remaining);
    }

    /// <summary>PRD 8 - Hücum edən komanda bombanı zərərsizləşdirmir.</summary>
    [Fact]
    public void DefuseKit_IsNotSoldToAttackers()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Alpha, isAlive: true, buyEnabled: true,
            alreadyOwned: false, money: Rich, out int remaining);

        Assert.Equal(BuyResultCode.WrongTeam, code);
        Assert.Equal(Rich, remaining);
    }

    [Fact]
    public void Buying_OutsideBuyPhase_IsRejected()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: true, buyEnabled: false,
            alreadyOwned: false, money: Rich, out int remaining);

        Assert.Equal(BuyResultCode.NotInBuyPhase, code);
        Assert.Equal(Rich, remaining);
    }

    [Fact]
    public void Buying_WhileDead_IsRejected()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: false, buyEnabled: true,
            alreadyOwned: false, money: Rich, out _);

        Assert.Equal(BuyResultCode.PlayerDead, code);
    }

    [Fact]
    public void Buying_Duplicate_IsRejected()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: true, buyEnabled: true,
            alreadyOwned: true, money: Rich, out int remaining);

        Assert.Equal(BuyResultCode.AlreadyOwned, code);
        Assert.Equal(Rich, remaining);
    }

    /// <summary>Pul çatmayanda balans toxunulmaz qalmalıdır.</summary>
    [Fact]
    public void Buying_WithoutMoney_LeavesBalanceUntouched()
    {
        int poor = GameConstants.DefuseKitPrice - 1;

        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: true, buyEnabled: true,
            alreadyOwned: false, money: poor, out int remaining);

        Assert.Equal(BuyResultCode.NotEnoughMoney, code);
        Assert.Equal(poor, remaining);
    }

    [Fact]
    public void ExactMoney_IsEnough()
    {
        BuyResultCode code = BuyRules.Evaluate(
            BuyItem.DefuseKit, Team.Bravo, isAlive: true, buyEnabled: true,
            alreadyOwned: false, money: GameConstants.DefuseKitPrice, out int remaining);

        Assert.Equal(BuyResultCode.Purchased, code);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void UnknownItem_IsRejected()
    {
        BuyResultCode code = BuyRules.Evaluate(
            (BuyItem)200, Team.Bravo, isAlive: true, buyEnabled: true,
            alreadyOwned: false, money: Rich, out _);

        Assert.Equal(BuyResultCode.UnknownItem, code);
    }

    /// <summary>Buy fazası round idarəçisi ilə uzlaşmalıdır (PRD 10, 27).</summary>
    [Theory]
    [InlineData(RoundPhase.FreezeTime, true)]
    [InlineData(RoundPhase.BuyTime, true)]
    [InlineData(RoundPhase.Active, false)]
    [InlineData(RoundPhase.BombPlanted, false)]
    [InlineData(RoundPhase.RoundEnd, false)]
    public void BuyWindow_MatchesRoundPhase(RoundPhase phase, bool expected)
    {
        // MatchDirector.BuyEnabled ilə eyni qayda; sənəd kimi burada da kilidlənir.
        bool buyEnabled = phase is RoundPhase.FreezeTime or RoundPhase.BuyTime;

        Assert.Equal(expected, buyEnabled);
    }

    [Theory]
    [InlineData(BuyItem.DefuseKit, BuyResultCode.Purchased, 1234)]
    [InlineData(BuyItem.DefuseKit, BuyResultCode.NotEnoughMoney, 0)]
    public void BuyResult_SurvivesWireRoundTrip(BuyItem item, BuyResultCode code, int money)
    {
        byte[] packet = PacketCodec.EncodeBuyResult(item, code, money);

        Assert.Equal(MessageType.BuyResult, PacketCodec.ReadType(packet));
        var decoded = PacketCodec.DecodeBuyResult(PacketCodec.Payload(packet));
        Assert.Equal(item, decoded.Item);
        Assert.Equal(code, decoded.Code);
        Assert.Equal(money, decoded.Money);
    }

    [Fact]
    public void BuyRequest_SurvivesWireRoundTrip()
    {
        byte[] packet = PacketCodec.EncodeBuyRequest(BuyItem.DefuseKit);

        Assert.Equal(MessageType.BuyRequest, PacketCodec.ReadType(packet));
        Assert.Equal(BuyItem.DefuseKit, PacketCodec.DecodeBuyRequest(PacketCodec.Payload(packet)));
    }

    [Fact]
    public void BuyPayloads_RejectTruncatedInput()
    {
        Assert.Throws<ArgumentException>(() => PacketCodec.DecodeBuyRequest(ReadOnlySpan<byte>.Empty));
        Assert.Throws<ArgumentException>(() => PacketCodec.DecodeBuyResult(new byte[] { 0, 0 }));
    }
}
