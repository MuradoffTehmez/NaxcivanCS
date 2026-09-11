// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Bir hit-in hesablanmış nəticəsi.</summary>
public readonly record struct DamageResult(int HealthDamage, int ArmorDamage, bool IsLethal);

/// <summary>
/// PRD 19, 20 - Damage, hitbox multiplier, armor və falloff hesablaması.
/// PRD 46, 156 - Yalnız server bu hesablamanı aparır.
/// </summary>
public static class DamageRules
{
    /// <summary>Armor mövcud olduqda health-ə düşən damage payı.</summary>
    private const float ArmorAbsorbRatio = 0.5f;

    public static DamageResult Calculate(
        WeaponData weapon,
        HitBox hitBox,
        float distanceMeters,
        int targetHealth,
        int targetArmor,
        ArmorType armorType)
    {
        float damage = weapon.Damage * GameConstants.HitBoxMultiplier(hitBox);
        damage *= FalloffMultiplier(weapon, distanceMeters);

        int armorDamage = 0;
        bool armorProtects = targetArmor > 0
            && (hitBox != HitBox.Head || armorType == ArmorType.VestAndHelmet);

        if (armorProtects)
        {
            // ArmorPenetration 1.0 = armor damage-i azaltmir, 0.0 = maksimum azaldir.
            float reduction = (1f - weapon.ArmorPenetration) * ArmorAbsorbRatio;
            float absorbed = damage * reduction;
            damage -= absorbed;
            armorDamage = Math.Min(targetArmor, (int)MathF.Round(absorbed));
        }

        int healthDamage = Math.Max(0, (int)MathF.Round(damage));
        return new DamageResult(healthDamage, armorDamage, healthDamage >= targetHealth);
    }

    /// <summary>Range-dən sonra hər metr üçün tədricən azalan damage.</summary>
    public static float FalloffMultiplier(WeaponData weapon, float distanceMeters)
    {
        if (distanceMeters <= weapon.Range || weapon.DamageFalloff <= 0f)
        {
            return 1f;
        }

        float excess = distanceMeters - weapon.Range;
        return Math.Clamp(1f - (excess * weapon.DamageFalloff), 0.1f, 1f);
    }

    /// <summary>PRD 18 - Cari spread (stance + hərəkət + recoil).</summary>
    public static float CalculateSpread(WeaponData weapon, Stance stance, float velocityRatio, int shotsInBurst)
    {
        float baseSpread = stance switch
        {
            Stance.Airborne => weapon.SpreadJumping,
            Stance.Running or Stance.Walking => weapon.SpreadMoving,
            _ => weapon.SpreadStanding,
        };

        float stanceMultiplier = GameConstants.StanceSpreadMultiplier(stance);
        float movementPenalty = 1f + Math.Clamp(velocityRatio, 0f, 1f);
        float burstPenalty = 1f + (shotsInBurst * 0.05f);

        return baseSpread * stanceMultiplier * movementPenalty * burstPenalty;
    }
}
