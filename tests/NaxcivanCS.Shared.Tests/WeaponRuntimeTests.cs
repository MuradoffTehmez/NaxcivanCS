using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>
/// PRD 14, 17, 47 - Silahin server terefdeki davranisi.
/// Kadensiyani server teyin edir; client atesh suretini deyise bilmir.
/// </summary>
public sealed class WeaponRuntimeTests
{
    private const float Tick = GameConstants.ServerTickIntervalSeconds;

    private static WeaponData Rifle(int fireRate = 600, int magazine = 30, bool automatic = true) => new()
    {
        Id = "test_rifle",
        Name = "Test Rifle",
        Category = WeaponCategory.Rifle,
        Damage = 34,
        FireRate = fireRate,
        MagazineSize = magazine,
        ReserveAmmo = 90,
        ReloadTime = 2.4f,
        Automatic = automatic,
        RecoilPattern = new[]
        {
            new RecoilStep(1.0f, 0f),
            new RecoilStep(1.5f, 0.3f),
            new RecoilStep(2.0f, -0.4f),
        },
    };

    /// <summary>Verilmis muddet erzinde tetik basili saxlayib atishlari sayir.</summary>
    private static int FireForSeconds(WeaponRuntime runtime, float seconds, bool fireHeld = true)
    {
        int shots = 0;
        int ticks = (int)(seconds / Tick);

        for (int i = 0; i < ticks; i++)
        {
            if (runtime.Tick(fireHeld, reloadRequested: false, Tick) == WeaponAction.Fire)
            {
                shots++;
            }
        }

        return shots;
    }

    [Fact]
    public void HoldingTrigger_FiresAtWeaponCadence_NotAtTickRate()
    {
        // REQRESSIYA TESTI.
        // Kohne dizayn her input paketini ayrica atish cehdi sayirdi:
        // 64 tick/saniye input, 600 RPM silah -> saniyede ~54 yalanchi
        // "mumkun olmayan atesh sureti" pozuntusu. Qanuni oyuncu bir
        // saniyeden az muddetde anti-cheat heddini kecirdi.
        var runtime = new WeaponRuntime(Rifle(fireRate: 600, magazine: 100));

        int shots = FireForSeconds(runtime, seconds: 1f);

        // 600 RPM = saniyede 10 atish (tick diskretliyine gore +-1).
        Assert.InRange(shots, 9, 11);
    }

    [Fact]
    public void HigherFireRate_ProducesMoreShots()
    {
        var slow = new WeaponRuntime(Rifle(fireRate: 300, magazine: 100));
        var fast = new WeaponRuntime(Rifle(fireRate: 900, magazine: 100));

        Assert.True(FireForSeconds(fast, 1f) > FireForSeconds(slow, 1f));
    }

    [Fact]
    public void SemiAutomatic_RequiresTriggerRelease()
    {
        var runtime = new WeaponRuntime(Rifle(fireRate: 600, magazine: 100, automatic: false));

        // Tetik basili saxlanilir -> yalniz BIR atish.
        Assert.Equal(1, FireForSeconds(runtime, 1f));

        // Buraxib yeniden basmaq ikinci atishi verir.
        runtime.Tick(fireHeld: false, reloadRequested: false, Tick);
        Assert.Equal(WeaponAction.Fire, runtime.Tick(fireHeld: true, reloadRequested: false, Tick));
    }

    [Fact]
    public void Firing_ConsumesMagazine()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 5));
        int shots = FireForSeconds(runtime, 1f);

        Assert.Equal(5, shots);
        Assert.Equal(0, runtime.AmmoInMagazine);
    }

    [Fact]
    public void EmptyMagazine_TriggersAutomaticReload()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 3));
        FireForSeconds(runtime, 1f);

        Assert.Equal(0, runtime.AmmoInMagazine);
        Assert.True(runtime.IsReloading);
    }

    [Fact]
    public void Reload_RefillsFromReserve()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 5));
        FireForSeconds(runtime, 1f);

        // Reload muddeti bitene qeder tick.
        WeaponAction last = WeaponAction.None;
        for (int i = 0; i < (int)(3.0f / Tick); i++)
        {
            WeaponAction action = runtime.Tick(fireHeld: false, reloadRequested: false, Tick);
            if (action == WeaponAction.ReloadFinished)
            {
                last = action;
                break;
            }
        }

        Assert.Equal(WeaponAction.ReloadFinished, last);
        Assert.Equal(5, runtime.AmmoInMagazine);
        Assert.Equal(85, runtime.ReserveAmmo);
    }

    [Fact]
    public void Reload_IsRejectedWhenMagazineIsFull()
        => Assert.False(new WeaponRuntime(Rifle()).TryStartReload());

    [Fact]
    public void CannotFireWhileReloading()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 10));
        FireForSeconds(runtime, 0.3f);
        Assert.True(runtime.TryStartReload());

        int shotsDuringReload = 0;
        for (int i = 0; i < (int)(1.0f / Tick); i++)
        {
            if (runtime.Tick(fireHeld: true, reloadRequested: false, Tick) == WeaponAction.Fire)
            {
                shotsDuringReload++;
            }
        }

        Assert.Equal(0, shotsDuringReload);
    }

    [Fact]
    public void FirstShotOfASpray_IsPerfectlyAccurate()
    {
        // PRD 17, 18 - Taktiki FPS-in temel qaydasi: dayanib bir defe atesh
        // acan oyuncu crosshair-in getdiyi yere vurmalidir.
        //
        // REQRESSIYA: evvel punch atishdan EVVEL irelileyirdi, yeni ilk gulle
        // de 0.65 derece sapma dasiyirdi. 24 metrde bu 27 sm demekdir - gulle
        // eyni boyda hedefin basinin ustunden kecirdi.
        var runtime = new WeaponRuntime(Rifle(magazine: 30));

        Assert.Equal(WeaponAction.Fire, runtime.Tick(fireHeld: true, reloadRequested: false, Tick));
        Assert.Equal(RecoilPunch.Zero, runtime.LastShotPunch);

        // Ikinci gulle artiq birinci addimin sapmasini dasiyir.
        int ticks = (int)(Rifle().ShotInterval / Tick) + 1;
        for (int i = 0; i < ticks; i++)
        {
            runtime.Tick(fireHeld: true, reloadRequested: false, Tick);
        }

        Assert.True(runtime.LastShotPunch.Pitch > 0f);
    }

    [Fact]
    public void FirstShotAfterSprayReset_IsAccurateAgain()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 30));
        FireForSeconds(runtime, 0.5f);
        Assert.True(runtime.LastShotPunch.Pitch > 0f);

        // Tetiyi burax, spray sifirlansin.
        for (int i = 0; i < (int)(1.0f / Tick); i++)
        {
            runtime.Tick(fireHeld: false, reloadRequested: false, Tick);
        }

        Assert.Equal(WeaponAction.Fire, runtime.Tick(fireHeld: true, reloadRequested: false, Tick));
        Assert.Equal(0f, runtime.LastShotPunch.Magnitude, 4);
    }

    [Fact]
    public void Recoil_ClimbsWhileSpraying()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 30));

        float startPitch = runtime.Punch.Pitch;
        FireForSeconds(runtime, 0.5f);

        // PRD 17 - spray zamani crosshair yuxari qalxir.
        Assert.True(runtime.Punch.Pitch > startPitch);
        Assert.True(runtime.ShotIndex > 1);
    }

    [Fact]
    public void Recoil_RecoversAfterTriggerRelease()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 30));
        FireForSeconds(runtime, 0.5f);

        float peak = runtime.Punch.Magnitude;
        Assert.True(peak > 0f);

        for (int i = 0; i < (int)(1.5f / Tick); i++)
        {
            runtime.Tick(fireHeld: false, reloadRequested: false, Tick);
        }

        Assert.True(runtime.Punch.Magnitude < peak);
    }

    [Fact]
    public void Spray_ResetsAfterPause()
    {
        var runtime = new WeaponRuntime(Rifle(magazine: 30));
        FireForSeconds(runtime, 0.3f);
        Assert.True(runtime.ShotIndex > 0);

        for (int i = 0; i < (int)(1.0f / Tick); i++)
        {
            runtime.Tick(fireHeld: false, reloadRequested: false, Tick);
        }

        Assert.Equal(0, runtime.ShotIndex);
    }
}

/// <summary>PRD 17 - Recoil pattern-i deterministik olmalidir.</summary>
public sealed class RecoilCalculatorTests
{
    private static readonly WeaponData Weapon = new()
    {
        Id = "w",
        Name = "W",
        Category = WeaponCategory.Rifle,
        RecoilPattern = new[]
        {
            new RecoilStep(1.0f, 0f),
            new RecoilStep(1.5f, 0.5f),
            new RecoilStep(2.0f, -0.5f),
        },
    };

    [Fact]
    public void SameShotIndex_AlwaysGivesSameStep()
    {
        // Oyuncunun pattern oyrenmesi mehz buna esaslanir (PRD 17).
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(RecoilCalculator.StepFor(Weapon, 1), RecoilCalculator.StepFor(Weapon, 1));
        }
    }

    [Fact]
    public void PatternRepeatsLastStepWhenExhausted()
    {
        RecoilStep last = RecoilCalculator.StepFor(Weapon, Weapon.RecoilPattern.Count - 1);
        Assert.Equal(last, RecoilCalculator.StepFor(Weapon, 99));
    }

    [Fact]
    public void Advance_AccumulatesVerticalClimb()
    {
        RecoilPunch punch = RecoilPunch.Zero;
        punch = RecoilCalculator.Advance(punch, Weapon, 0);
        float afterFirst = punch.Pitch;

        punch = RecoilCalculator.Advance(punch, Weapon, 1);

        Assert.True(afterFirst > 0f);
        Assert.True(punch.Pitch > afterFirst);
    }

    [Fact]
    public void Recover_DoesNothingDuringTheDelayWindow()
    {
        var punch = new RecoilPunch(5f, 1f);
        RecoilPunch after = RecoilCalculator.Recover(punch, secondsSinceLastShot: 0.05f, deltaSeconds: 0.016f);

        Assert.Equal(punch, after);
    }

    [Fact]
    public void Recover_ReachesZeroEventually()
    {
        var punch = new RecoilPunch(5f, 1f);

        for (int i = 0; i < 200; i++)
        {
            punch = RecoilCalculator.Recover(punch, secondsSinceLastShot: 1f, deltaSeconds: 0.016f);
        }

        Assert.Equal(0f, punch.Magnitude, 4);
    }

    [Fact]
    public void WeaponWithoutPattern_ProducesNoRecoil()
    {
        var knife = new WeaponData { Id = "k", Name = "K", Category = WeaponCategory.Knife };
        Assert.Equal(RecoilPunch.Zero, RecoilCalculator.Advance(RecoilPunch.Zero, knife, 0));
    }
}
