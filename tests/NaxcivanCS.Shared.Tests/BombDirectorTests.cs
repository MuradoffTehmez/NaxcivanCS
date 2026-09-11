// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 8 - Plant/defuse qaydaları.</summary>
public sealed class BombDirectorTests
{
    private const int Carrier = 1;
    private const int Defender = 2;
    private const float Tick = GameConstants.ServerTickIntervalSeconds;

    /// <summary>
    /// İrəliləyiş hər tick-də float olaraq toplanır, ona görə dəqiq sərhəddə
    /// bir tick gecikə bilər. Tamamlanma gözlənilən yerlərdə bu tolerantlıq verilir.
    /// </summary>
    private const float CompletionSlack = Tick;

    /// <summary>Site A-nın mərkəzi — plant üçün etibarlı nöqtə.</summary>
    private static Vector3 SiteCenter => BlockoutMap.Sites[0].Center with { Y = 0.1f };

    private static Vector3 OutsideSite => new(0f, 0.1f, -10f);

    private static BombInteractor Attacker(Vector3 position, bool interacting = true, int peerId = Carrier)
        => new(peerId, Team.Alpha, true, position, interacting, false);

    private static BombInteractor Defuser(Vector3 position, bool interacting = true, bool kit = false)
        => new(Defender, Team.Bravo, true, position, interacting, kit);

    private static BombDirector Started(Vector3 at)
    {
        var bomb = new BombDirector();
        bomb.BeginRound(Carrier, at);
        return bomb;
    }

    /// <summary>
    /// Tick edir və ilk həlledici hadisədə dayanır. İrəliləyiş hadisələri
    /// dayandırmır, əks halda vaxt bitənə qədər davam edir.
    /// </summary>
    private static BombEvent Run(
        BombDirector bomb, RoundPhase phase, float seconds, params BombInteractor[] players)
    {
        BombEvent last = BombEvent.None;
        int ticks = (int)MathF.Ceiling(seconds / Tick);

        for (int i = 0; i < ticks; i++)
        {
            last = bomb.Tick(Tick, phase, players);

            if (last is not (BombEvent.None or BombEvent.PlantProgressed or BombEvent.DefuseProgressed))
            {
                break;
            }
        }

        return last;
    }

    [Fact]
    public void BeginRound_GivesBombToCarrier()
    {
        BombDirector bomb = Started(OutsideSite);

        Assert.Equal(BombState.Carried, bomb.State);
        Assert.Equal(Carrier, bomb.CarrierPeerId);
        Assert.Equal(0f, bomb.PlantProgress);
    }

    [Fact]
    public void Plant_InsideSite_CompletesAfterPlantTime()
    {
        BombDirector bomb = Started(SiteCenter);

        BombEvent result = Run(
            bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds + CompletionSlack, Attacker(SiteCenter));

        Assert.Equal(BombEvent.Planted, result);
        Assert.Equal(BombState.Planted, bomb.State);
        Assert.Equal("A", bomb.PlantedSite);
        Assert.Equal(Carrier, bomb.PlanterPeerId);
        Assert.Equal(0, bomb.CarrierPeerId);
    }

    [Fact]
    public void Plant_OutsideSite_MakesNoProgress()
    {
        BombDirector bomb = Started(OutsideSite);

        BombEvent result = Run(
            bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds * 2f, Attacker(OutsideSite));

        Assert.Equal(BombEvent.None, result);
        Assert.Equal(BombState.Carried, bomb.State);
        Assert.Equal(0f, bomb.PlantProgress);
    }

    [Theory]
    [InlineData(RoundPhase.FreezeTime)]
    [InlineData(RoundPhase.BuyTime)]
    [InlineData(RoundPhase.RoundEnd)]
    public void Plant_OutsideActivePhase_IsRejected(RoundPhase phase)
    {
        BombDirector bomb = Started(SiteCenter);

        Run(bomb, phase, GameConstants.PlantTimeSeconds * 2f, Attacker(SiteCenter));

        Assert.Equal(BombState.Carried, bomb.State);
        Assert.Equal(0f, bomb.PlantProgress);
    }

    [Fact]
    public void Plant_ReleasingInteract_ResetsProgress()
    {
        BombDirector bomb = Started(SiteCenter);
        Run(bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds / 2f, Attacker(SiteCenter));
        Assert.True(bomb.PlantProgress > 0f);

        BombEvent cancelled = bomb.Tick(
            Tick, RoundPhase.Active, new[] { Attacker(SiteCenter, interacting: false) });

        Assert.Equal(BombEvent.PlantCancelled, cancelled);
        Assert.Equal(0f, bomb.PlantProgress);
        Assert.Equal(BombState.Carried, bomb.State);
    }

    [Fact]
    public void Plant_LeavingSite_ResetsProgress()
    {
        BombDirector bomb = Started(SiteCenter);
        Run(bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds / 2f, Attacker(SiteCenter));

        bomb.Tick(Tick, RoundPhase.Active, new[] { Attacker(OutsideSite) });

        Assert.Equal(0f, bomb.PlantProgress);
        Assert.Equal(BombState.Carried, bomb.State);
    }

    [Fact]
    public void CarrierDeath_DropsBombAndCancelsPlant()
    {
        BombDirector bomb = Started(SiteCenter);
        Run(bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds / 2f, Attacker(SiteCenter));

        BombEvent dropped = bomb.OnCarrierDied(SiteCenter);

        Assert.Equal(BombEvent.Dropped, dropped);
        Assert.Equal(BombState.Dropped, bomb.State);
        Assert.Equal(0, bomb.CarrierPeerId);
        Assert.Equal(0f, bomb.PlantProgress);
    }

    [Fact]
    public void DroppedBomb_IsPickedUpByNearbyAttacker()
    {
        BombDirector bomb = Started(SiteCenter);
        bomb.OnCarrierDied(SiteCenter);

        BombEvent picked = bomb.Tick(
            Tick, RoundPhase.Active, new[] { Attacker(SiteCenter, peerId: 7) });

        Assert.Equal(BombEvent.PickedUp, picked);
        Assert.Equal(BombState.Carried, bomb.State);
        Assert.Equal(7, bomb.CarrierPeerId);
    }

    [Fact]
    public void DroppedBomb_IsNotPickedUpByDefender()
    {
        BombDirector bomb = Started(SiteCenter);
        bomb.OnCarrierDied(SiteCenter);

        bomb.Tick(Tick, RoundPhase.Active, new[] { Defuser(SiteCenter) });

        Assert.Equal(BombState.Dropped, bomb.State);
        Assert.Equal(0, bomb.CarrierPeerId);
    }

    [Fact]
    public void DroppedBomb_OutOfReach_StaysOnGround()
    {
        BombDirector bomb = Started(SiteCenter);
        bomb.OnCarrierDied(SiteCenter);
        Vector3 far = SiteCenter + new Vector3(GameConstants.BombPickupRadiusMeters + 1f, 0f, 0f);

        bomb.Tick(Tick, RoundPhase.Active, new[] { Attacker(far, peerId: 7) });

        Assert.Equal(BombState.Dropped, bomb.State);
    }

    [Fact]
    public void Defuse_WithoutKit_TakesFullDefuseTime()
    {
        BombDirector bomb = Planted();

        BombEvent almost = Run(
            bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeWithKitSeconds, Defuser(SiteCenter));
        Assert.Equal(BombEvent.DefuseProgressed, almost);
        Assert.Equal(BombState.Planted, bomb.State);

        BombEvent done = Run(
            bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds + CompletionSlack, Defuser(SiteCenter));

        Assert.Equal(BombEvent.Defused, done);
        Assert.Equal(BombState.Defused, bomb.State);
        Assert.Equal(Defender, bomb.DefuserPeerId);
    }

    [Fact]
    public void Defuse_WithKit_IsTwiceAsFast()
    {
        BombDirector bomb = Planted();

        BombEvent done = Run(
            bomb,
            RoundPhase.BombPlanted,
            GameConstants.DefuseTimeWithKitSeconds + CompletionSlack,
            Defuser(SiteCenter, kit: true));

        Assert.Equal(BombEvent.Defused, done);
        Assert.Equal(BombState.Defused, bomb.State);
    }

    [Fact]
    public void Defuse_ByAttacker_IsRejected()
    {
        BombDirector bomb = Planted();

        Run(bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds * 2f, Attacker(SiteCenter));

        Assert.Equal(BombState.Planted, bomb.State);
        Assert.Equal(0f, bomb.DefuseProgress);
    }

    [Fact]
    public void Defuse_TooFarAway_MakesNoProgress()
    {
        BombDirector bomb = Planted();
        Vector3 far = SiteCenter + new Vector3(GameConstants.DefuseRadiusMeters + 1f, 0f, 0f);

        Run(bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds, Defuser(far));

        Assert.Equal(BombState.Planted, bomb.State);
        Assert.Equal(0f, bomb.DefuseProgress);
    }

    [Fact]
    public void Defuse_Interrupted_ResetsProgress()
    {
        BombDirector bomb = Planted();
        Run(bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds / 2f, Defuser(SiteCenter));
        Assert.True(bomb.DefuseProgress > 0f);

        BombEvent cancelled = bomb.Tick(
            Tick, RoundPhase.BombPlanted, new[] { Defuser(SiteCenter, interacting: false) });

        Assert.Equal(BombEvent.DefuseCancelled, cancelled);
        Assert.Equal(0f, bomb.DefuseProgress);
    }

    /// <summary>Yarımçıq defuse başqa müdafiəçiyə ötürülə bilməz.</summary>
    [Fact]
    public void Defuse_SwitchingDefender_RestartsProgress()
    {
        BombDirector bomb = Planted();
        Run(bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds / 2f, Defuser(SiteCenter));
        float halfway = bomb.DefuseProgress;

        var other = new BombInteractor(9, Team.Bravo, true, SiteCenter, true, false);
        bomb.Tick(Tick, RoundPhase.BombPlanted, new[] { other });

        Assert.True(bomb.DefuseProgress < halfway);
        Assert.Equal(BombState.Planted, bomb.State);
    }

    [Fact]
    public void TimerExpired_ExplodesPlantedBomb()
    {
        BombDirector bomb = Planted();

        BombEvent exploded = bomb.OnTimerExpired();

        Assert.Equal(BombEvent.Exploded, exploded);
        Assert.Equal(BombState.Exploded, bomb.State);
    }

    [Fact]
    public void TimerExpired_AfterDefuse_DoesNothing()
    {
        BombDirector bomb = Planted();
        Run(bomb, RoundPhase.BombPlanted, GameConstants.DefuseTimeSeconds + CompletionSlack, Defuser(SiteCenter));
        Assert.Equal(BombState.Defused, bomb.State);

        Assert.Equal(BombEvent.None, bomb.OnTimerExpired());
        Assert.Equal(BombState.Defused, bomb.State);
    }

    [Fact]
    public void PlantedBomb_IsNotAffectedByCarrierDeath()
    {
        BombDirector bomb = Planted();

        Assert.Equal(BombEvent.None, bomb.OnCarrierDied(OutsideSite));
        Assert.Equal(BombState.Planted, bomb.State);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    public void SiteAt_ReportsBothSites(string siteName)
    {
        BombSite site = BlockoutMap.Sites.Single(s => s.Name == siteName);

        Assert.Equal(siteName, BlockoutMap.SiteAt(site.Center));
    }

    [Fact]
    public void SiteAt_OutsideAnySite_IsNull()
    {
        Assert.Null(BlockoutMap.SiteAt(OutsideSite));
        Assert.Null(BlockoutMap.SiteAt(BlockoutMap.SpawnPosition(Team.Alpha, 0)));
    }

    [Theory]
    [InlineData(BombState.Carried, 4, "")]
    [InlineData(BombState.Planted, 0, "A")]
    [InlineData(BombState.Planted, 0, "B")]
    [InlineData(BombState.Defused, 0, "B")]
    public void BombState_SurvivesWireRoundTrip(BombState state, int carrier, string site)
    {
        var position = new Vector3(-9.25f, 0.1f, 7.5f);

        byte[] packet = PacketCodec.EncodeBombState(state, carrier, position, 0.25f, 0.75f, site);

        Assert.Equal(MessageType.BombStateChanged, PacketCodec.ReadType(packet));
        var decoded = PacketCodec.DecodeBombState(PacketCodec.Payload(packet));
        Assert.Equal(state, decoded.State);
        Assert.Equal(carrier, decoded.CarrierPeerId);
        Assert.Equal(position, decoded.Position);
        Assert.Equal(0.25f, decoded.PlantProgress);
        Assert.Equal(0.75f, decoded.DefuseProgress);
        Assert.Equal(site, decoded.PlantedSite);
    }

    [Fact]
    public void BombState_ProgressIsClampedOnTheWire()
    {
        byte[] packet = PacketCodec.EncodeBombState(
            BombState.Planted, 0, Vector3.Zero, 5f, -2f, "A");

        var decoded = PacketCodec.DecodeBombState(PacketCodec.Payload(packet));
        Assert.Equal(1f, decoded.PlantProgress);
        Assert.Equal(0f, decoded.DefuseProgress);
    }

    [Fact]
    public void BombState_TruncatedPayloadIsRejected()
    {
        byte[] packet = PacketCodec.EncodeBombState(
            BombState.Planted, 0, Vector3.Zero, 1f, 0f, "A");
        byte[] truncated = PacketCodec.Payload(packet)[..10].ToArray();

        Assert.Throws<ArgumentException>(() => PacketCodec.DecodeBombState(truncated));
    }

    private static BombDirector Planted()
    {
        BombDirector bomb = Started(SiteCenter);
        Run(bomb, RoundPhase.Active, GameConstants.PlantTimeSeconds + CompletionSlack, Attacker(SiteCenter));
        Assert.Equal(BombState.Planted, bomb.State);
        return bomb;
    }
}
