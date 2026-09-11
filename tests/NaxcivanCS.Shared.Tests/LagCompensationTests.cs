using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 45, 46 - Lag compensation ve hitscan testleri.</summary>
public sealed class LagCompensationTests
{
    [Fact]
    public void Rewind_InterpolatesBetweenRecordedPositions()
    {
        var buffer = new LagCompensationBuffer();
        buffer.Record(1000, new Vector3(0f, 0f, 0f), 0f, false);
        buffer.Record(1100, new Vector3(10f, 0f, 0f), 0f, false);

        PositionRecord? rewound = buffer.Rewind(1050);

        Assert.NotNull(rewound);
        Assert.Equal(5f, rewound!.Value.Position.X, 2);
    }

    [Fact]
    public void Rewind_ReturnsNullWhenHistoryIsEmpty()
        => Assert.Null(new LagCompensationBuffer().Rewind(1000));

    [Fact]
    public void Rewind_ClampsToOldestAndNewestRecords()
    {
        var buffer = new LagCompensationBuffer();
        buffer.Record(1000, new Vector3(1f, 0f, 0f), 0f, false);
        buffer.Record(1100, new Vector3(9f, 0f, 0f), 0f, false);

        Assert.Equal(1f, buffer.Rewind(500)!.Value.Position.X);
        Assert.Equal(9f, buffer.Rewind(5000)!.Value.Position.X);
    }

    [Fact]
    public void History_IsTrimmedToTheConfiguredWindow()
    {
        var buffer = new LagCompensationBuffer(windowMs: 200);

        for (int i = 0; i <= 100; i++)
        {
            buffer.Record(i * 10, new Vector3(i, 0f, 0f), 0f, false);
        }

        // PRD 45 - ~200 ms tarixce saxlanilir, daha coxu yox.
        Assert.True(buffer.SpanMs <= 200);
        Assert.True(buffer.Count <= 22);
    }

    [Fact]
    public void RewindTime_IsClampedToMaxWindow()
    {
        // Suni sekilde boyuk latency bildiren client daha cox geriye sara bilmemelidir.
        double rewind = LagCompensationBuffer.ResolveRewindTime(
            serverTimeMs: 10_000,
            shooterLatencyMs: 5_000);

        Assert.Equal(10_000 - GameConstants.LagCompensationHistoryMs, rewind);
    }

    [Fact]
    public void RewindTime_IgnoresNegativeLatency()
        => Assert.Equal(10_000, LagCompensationBuffer.ResolveRewindTime(10_000, -500));
}

/// <summary>PRD 19, 46 - Hitscan hendesesi.</summary>
public sealed class HitScanTests
{
    private static readonly Vector3 Eye = new(0f, HitScan.EyeHeight(crouching: false), 0f);

    [Fact]
    public void EyeLevelShot_HitsTheHead()
    {
        // Goz seviyyesine duz atis = headshot. Competitive FPS-de crosshair
        // placement mexanikasi mehz budur (PRD 19) — hedef eyni boydadirsa
        // ufuqi atis basa deyir.
        HitScanResult result = HitScan.Intersect(Eye, -Vector3.UnitZ, new Vector3(0f, 0f, -10f), crouching: false);

        Assert.True(result.Hit);
        Assert.Equal(10f, result.Distance, 1);
        Assert.Equal(HitBox.Head, result.HitBox);
    }

    [Fact]
    public void ChestHeightShot_HitsTheChest()
    {
        var target = new Vector3(0f, 0f, -10f);
        Vector3 direction = Vector3.Normalize(new Vector3(0f, 1.3f, -10f) - Eye);

        HitScanResult result = HitScan.Intersect(Eye, direction, target, crouching: false);

        Assert.True(result.Hit);
        Assert.Equal(HitBox.Chest, result.HitBox);
    }

    [Fact]
    public void UpwardAngle_HitsTheHead()
    {
        // 10 m irelide duran hedefin bas seviyyesine nisanlanir.
        var target = new Vector3(0f, 0f, -10f);
        var aimPoint = new Vector3(0f, HitScan.StandingHeight - 0.1f, -10f);
        Vector3 direction = Vector3.Normalize(aimPoint - Eye);

        HitScanResult result = HitScan.Intersect(Eye, direction, target, crouching: false);

        Assert.True(result.Hit);
        Assert.Equal(HitBox.Head, result.HitBox);
    }

    [Fact]
    public void DownwardAngle_HitsTheLegs()
    {
        var target = new Vector3(0f, 0f, -10f);
        var aimPoint = new Vector3(0f, 0.3f, -10f);
        Vector3 direction = Vector3.Normalize(aimPoint - Eye);

        HitScanResult result = HitScan.Intersect(Eye, direction, target, crouching: false);

        Assert.True(result.Hit);
        Assert.Equal(HitBox.Legs, result.HitBox);
    }

    [Fact]
    public void TargetBehindShooter_IsNotHit()
    {
        HitScanResult result = HitScan.Intersect(Eye, -Vector3.UnitZ, new Vector3(0f, 0f, 10f), crouching: false);
        Assert.False(result.Hit);
    }

    [Fact]
    public void ShotWideOfTheTarget_Misses()
    {
        HitScanResult result = HitScan.Intersect(Eye, -Vector3.UnitZ, new Vector3(3f, 0f, -10f), crouching: false);
        Assert.False(result.Hit);
    }

    [Fact]
    public void CrouchingTarget_IsHarderToHeadshotAtStandingHeight()
    {
        var target = new Vector3(0f, 0f, -10f);

        // Duz ireli atis: ayaqda duran hedefde dese deyir, comelmis hedefin ustunden kecir.
        HitScanResult standing = HitScan.Intersect(Eye, -Vector3.UnitZ, target, crouching: false);
        HitScanResult crouched = HitScan.Intersect(Eye, -Vector3.UnitZ, target, crouching: true);

        Assert.True(standing.Hit);
        Assert.False(crouched.Hit);
    }

    [Fact]
    public void ZeroDirection_IsRejected()
        => Assert.False(HitScan.Intersect(Eye, Vector3.Zero, new Vector3(0f, 0f, -10f), false).Hit);
}
