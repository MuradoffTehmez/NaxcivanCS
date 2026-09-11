using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 124 - Damage ve weapon stat testleri.</summary>
public sealed class DamageRulesTests
{
    private static readonly WeaponData Rifle = new()
    {
        Id = "weapon_rifle_01",
        Name = "AR-9 Qartal",
        Category = WeaponCategory.Rifle,
        Damage = 34,
        ArmorPenetration = 0.7f,
        FireRate = 600,
        MagazineSize = 30,
        Range = 32f,
        DamageFalloff = 0.012f,
        Price = 2700,
    };

    [Fact]
    public void Headshot_UsesFourTimesMultiplier()
    {
        DamageResult result = DamageRules.Calculate(Rifle, HitBox.Head, 10f, 100, 0, ArmorType.None);
        Assert.Equal(136, result.HealthDamage);
        Assert.True(result.IsLethal);
    }

    [Fact]
    public void LegShot_IsReduced()
    {
        DamageResult result = DamageRules.Calculate(Rifle, HitBox.Legs, 10f, 100, 0, ArmorType.None);
        Assert.Equal(26, result.HealthDamage); // 34 * 0.75 = 25.5 -> 26
    }

    [Fact]
    public void ChestShot_IsBaseline()
    {
        DamageResult result = DamageRules.Calculate(Rifle, HitBox.Chest, 5f, 100, 0, ArmorType.None);
        Assert.Equal(Rifle.Damage, result.HealthDamage);
    }

    [Fact]
    public void Armor_ReducesHealthDamage()
    {
        DamageResult unarmored = DamageRules.Calculate(Rifle, HitBox.Chest, 5f, 100, 0, ArmorType.None);
        DamageResult armored = DamageRules.Calculate(Rifle, HitBox.Chest, 5f, 100, 100, ArmorType.Vest);

        Assert.True(armored.HealthDamage < unarmored.HealthDamage);
        Assert.True(armored.ArmorDamage > 0);
    }

    [Fact]
    public void Helmet_ProtectsAgainstHeadshotButVestAloneDoesNot()
    {
        DamageResult vestOnly = DamageRules.Calculate(Rifle, HitBox.Head, 5f, 100, 100, ArmorType.Vest);
        DamageResult withHelmet = DamageRules.Calculate(Rifle, HitBox.Head, 5f, 100, 100, ArmorType.VestAndHelmet);

        Assert.True(withHelmet.HealthDamage < vestOnly.HealthDamage);
    }

    [Fact]
    public void Falloff_DoesNotApplyInsideRange()
        => Assert.Equal(1f, DamageRules.FalloffMultiplier(Rifle, 20f));

    [Fact]
    public void Falloff_ReducesDamageBeyondRange()
        => Assert.True(DamageRules.FalloffMultiplier(Rifle, 60f) < 1f);

    [Fact]
    public void Spread_IsWorstWhileAirborne()
    {
        WeaponData w = Rifle with { };
        float standing = DamageRules.CalculateSpread(w, Stance.Standing, 0f, 0);
        float airborne = DamageRules.CalculateSpread(w with { SpreadJumping = 0.28f }, Stance.Airborne, 1f, 0);

        Assert.True(airborne > standing);
    }

    [Fact]
    public void ShotInterval_MatchesFireRate()
        => Assert.Equal(0.1f, Rifle.ShotInterval, 3);
}
