// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Shared.AntiCheat;

/// <summary>PRD 47 - Server-side yoxlama pozuntuları.</summary>
public enum Violation
{
    ImpossibleMovement,
    SpeedHack,
    Teleport,
    ImpossibleFireRate,
    ImpossibleAmmo,
    InvalidWeapon,
    ImpossibleDamage,
    InvalidBuy,
    InvalidEconomy,
    SuspiciousAim,
    ImpossibleAngleChange,
    TamperedClient,
}

/// <summary>PRD 48 - SuspicionScore hesablanması. Avtomatik permanent ban YOXDUR.</summary>
public enum SuspicionAction
{
    None = 0,
    Review = 1,
    TemporaryRestriction = 2,
}

public static class SuspicionRules
{
    public static int Weight(Violation violation) => violation switch
    {
        Violation.ImpossibleMovement => 20,
        Violation.SpeedHack => 20,
        Violation.Teleport => 20,
        Violation.ImpossibleFireRate => 30,
        Violation.ImpossibleAmmo => 30,
        Violation.InvalidWeapon => 15,
        Violation.ImpossibleDamage => 15,
        Violation.InvalidBuy => 10,
        Violation.InvalidEconomy => 10,
        Violation.SuspiciousAim => 15,
        Violation.ImpossibleAngleChange => 15,
        Violation.TamperedClient => 40,
        _ => 0,
    };

    public static int Apply(int currentScore, Violation violation)
        => currentScore + Weight(violation);

    /// <summary>PRD 48 - 80 → review, 100 → müvəqqəti məhdudiyyət.</summary>
    public static SuspicionAction Evaluate(int score)
    {
        if (score >= GameConstants.SuspicionRestrictThreshold)
        {
            return SuspicionAction.TemporaryRestriction;
        }

        return score >= GameConstants.SuspicionReviewThreshold
            ? SuspicionAction.Review
            : SuspicionAction.None;
    }
}

/// <summary>PRD 47 - Movement və fire-rate üçün sərt server validasiyaları.</summary>
public static class ServerValidators
{
    /// <summary>Tick ərzində mümkün olan maksimum yerdəyişmə aşılıbsa true.</summary>
    public static bool IsImpossibleMovement(float distanceMoved, float maxSpeed, float deltaSeconds, float tolerance = 1.15f)
        => distanceMoved > maxSpeed * deltaSeconds * tolerance;

    /// <summary>Atışlar arası interval silahın icazə verdiyindən qısadırsa true.</summary>
    public static bool IsImpossibleFireRate(double secondsSinceLastShot, float weaponShotInterval, float tolerance = 0.9f)
        => secondsSinceLastShot < weaponShotInterval * tolerance;

    /// <summary>PRD 29 - Yerdən silah götürmək üçün məsafə yoxlaması.</summary>
    public static bool IsPickupInRange(float distanceMeters, float maxPickupDistance = 2.0f)
        => distanceMeters <= maxPickupDistance;
}
