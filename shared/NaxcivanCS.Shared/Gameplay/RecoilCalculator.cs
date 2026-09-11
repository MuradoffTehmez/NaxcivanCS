// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Kameraya və atış istiqamətinə tətbiq olunan cari recoil sapması (dərəcə).</summary>
public readonly record struct RecoilPunch(float Pitch, float Yaw)
{
    public static readonly RecoilPunch Zero = new(0f, 0f);

    public RecoilPunch Add(RecoilStep step, float scale)
        => new(Pitch + (step.Vertical * scale), Yaw + (step.Horizontal * scale));

    public RecoilPunch Scaled(float factor) => new(Pitch * factor, Yaw * factor);

    public float Magnitude => MathF.Sqrt((Pitch * Pitch) + (Yaw * Yaw));
}

/// <summary>
/// PRD 17 - Recoil sistemi.
///
/// <para>
/// <b>Recoil random deyil.</b> Hər silahın öyrənilə bilən pattern-i var
/// (<see cref="WeaponData.RecoilPattern"/>). Pattern bitdikdən sonra son addım
/// təkrarlanır — uzun spray-lər də proqnozlaşdırıla bilən qalır.
/// </para>
///
/// <para>
/// <b>Niyə burada, shared-də?</b> Recoil-i <b>server</b> hesablayır və atəş
/// istiqamətinə əlavə edir (PRD 156). Client eyni dəyəri yalnız kameranı
/// vizual olaraq qaldırmaq üçün istifadə edir. Beləliklə "no-recoil" hiylə
/// mənasızdır: client kameranı qaldırmasa da, güllə serverdə yenə sapır.
/// </para>
/// </summary>
public static class RecoilCalculator
{
    /// <summary>Pattern dəyərlərini dərəcəyə çevirən əmsal.</summary>
    public const float DegreesPerUnit = 0.65f;

    /// <summary>Atəş dayandıqdan sonra recoil-in sönmə sürəti (dərəcə/saniyə).</summary>
    public const float RecoveryDegreesPerSecond = 22f;

    /// <summary>Recovery başlamazdan əvvəl gözləmə (saniyə) — burst-lər arasında.</summary>
    public const float RecoveryDelaySeconds = 0.12f;

    /// <summary>
    /// Növbəti atış üçün recoil-i irəli aparır.
    /// </summary>
    /// <param name="current">Cari sapma.</param>
    /// <param name="weapon">Silah.</param>
    /// <param name="shotIndex">Bu spray-də neçənci atış (0-dan başlayır).</param>
    public static RecoilPunch Advance(RecoilPunch current, WeaponData weapon, int shotIndex)
    {
        ArgumentNullException.ThrowIfNull(weapon);

        if (weapon.RecoilPattern.Count == 0)
        {
            return current;
        }

        RecoilStep step = StepFor(weapon, shotIndex);
        return current.Add(step, DegreesPerUnit);
    }

    /// <summary>
    /// Pattern-in verilmiş atış üçün addımı. Pattern bitibsə son addım təkrarlanır.
    /// </summary>
    public static RecoilStep StepFor(WeaponData weapon, int shotIndex)
    {
        ArgumentNullException.ThrowIfNull(weapon);

        if (weapon.RecoilPattern.Count == 0)
        {
            return default;
        }

        int index = Math.Clamp(shotIndex, 0, weapon.RecoilPattern.Count - 1);
        return weapon.RecoilPattern[index];
    }

    /// <summary>
    /// Atəş dayandıqda recoil-i tədricən sıfıra qaytarır.
    /// </summary>
    /// <param name="current">Cari sapma.</param>
    /// <param name="secondsSinceLastShot">Son atışdan keçən vaxt.</param>
    /// <param name="deltaSeconds">Bu tick-in müddəti.</param>
    public static RecoilPunch Recover(RecoilPunch current, float secondsSinceLastShot, float deltaSeconds)
    {
        if (secondsSinceLastShot < RecoveryDelaySeconds)
        {
            return current;
        }

        float magnitude = current.Magnitude;
        if (magnitude <= 0.0001f)
        {
            return RecoilPunch.Zero;
        }

        float reduction = RecoveryDegreesPerSecond * deltaSeconds;
        if (reduction >= magnitude)
        {
            return RecoilPunch.Zero;
        }

        return current.Scaled((magnitude - reduction) / magnitude);
    }

    /// <summary>
    /// Spray davam edirmi? Fasilə bu həddi keçərsə pattern sıfırdan başlayır.
    /// </summary>
    public const float SprayResetSeconds = 0.35f;

    public static bool ShouldResetSpray(float secondsSinceLastShot)
        => secondsSinceLastShot >= SprayResetSeconds;
}
