using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Models;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 155 - Balans JSON-dan yuklenmelidir, kodda hardcode edilmemelidir.</summary>
public sealed class WeaponCatalogTests
{
    private static string ConfigDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "config", "weapons")))
            {
                dir = dir.Parent;
            }

            return dir is null
                ? throw new DirectoryNotFoundException("config/weapons tapilmadi")
                : Path.Combine(dir.FullName, "config", "weapons");
        }
    }

    [Fact]
    public void Catalog_LoadsMvpWeaponSet()
    {
        WeaponCatalog catalog = WeaponCatalog.LoadFromDirectory(ConfigDirectory);

        // PRD 136 - MVP ucun 6-8 silah.
        Assert.InRange(catalog.All.Count, 6, 8);
        Assert.True(catalog.Contains("weapon_rifle_01"));
    }

    [Fact]
    public void RifleConfig_MatchesPrdExample()
    {
        WeaponCatalog catalog = WeaponCatalog.LoadFromDirectory(ConfigDirectory);
        WeaponData rifle = catalog.Get("weapon_rifle_01")!;

        Assert.Equal(34, rifle.Damage);
        Assert.Equal(600, rifle.FireRate);
        Assert.Equal(30, rifle.MagazineSize);
        Assert.Equal(2.4f, rifle.ReloadTime, 3);
        Assert.Equal(2700, rifle.Price);
        Assert.Equal(WeaponCategory.Rifle, rifle.Category);
    }

    [Fact]
    public void EveryWeapon_HasIdNameAndNonNegativePrice()
    {
        WeaponCatalog catalog = WeaponCatalog.LoadFromDirectory(ConfigDirectory);

        foreach (WeaponData weapon in catalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(weapon.Id));
            Assert.False(string.IsNullOrWhiteSpace(weapon.Name));
            Assert.True(weapon.Price >= 0, $"{weapon.Id} price menfidir");
        }
    }

    [Fact]
    public void RecoilPattern_IsDeterministicForRifles()
    {
        WeaponCatalog catalog = WeaponCatalog.LoadFromDirectory(ConfigDirectory);

        foreach (WeaponData weapon in catalog.All.Where(w => w.Category == WeaponCategory.Rifle))
        {
            // PRD 17 - recoil random olmamalidir, predefined pattern olmalidir.
            Assert.NotEmpty(weapon.RecoilPattern);
        }
    }
}
