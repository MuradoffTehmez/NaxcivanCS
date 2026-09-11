using System.Numerics;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Shared.AntiCheat;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Server.Damage;

/// <summary>Serverin hesabladığı atəş nəticəsi.</summary>
public readonly record struct ShotResult(
    bool Accepted,
    int? VictimPeerId,
    HitBox HitBox,
    int HealthDamage,
    int ArmorDamage,
    bool Killed,
    Violation? Violation);

/// <summary>
/// PRD 45, 46, 47 - Atəşin serverdə validasiyası və hit hesablanması.
///
/// Client YALNIZ "atəş açdım + bu istiqamətə baxıram" deyir.
/// Kimin vurulduğunu və neçə damage dəydiyini <b>server</b> hesablayır.
/// Həndəsə <see cref="HitScan"/> (shared) sinfindədir — beləliklə unit testlərlə
/// Godot-suz yoxlanıla bilir.
/// </summary>
public sealed class HitValidator
{
    private readonly WeaponData _weapon;

    public HitValidator(WeaponData weapon) => _weapon = weapon;

    /// <summary>
    /// Atəşi qiymətləndirir. Fire-rate pozuntusu aşkarlanarsa atış rədd edilir
    /// və <see cref="ShotResult.Violation"/> doldurulur (PRD 47, 48).
    /// </summary>
    /// <param name="shooter">Atıcı.</param>
    /// <param name="aimDirection">Baxış istiqaməti.</param>
    /// <param name="candidates">Potensial hədəflər.</param>
    /// <param name="serverTimeMs">Cari server vaxtı.</param>
    public ShotResult Evaluate(
        ServerPlayer shooter,
        Vector3 aimDirection,
        IEnumerable<ServerPlayer> candidates,
        double serverTimeMs)
    {
        ArgumentNullException.ThrowIfNull(shooter);
        ArgumentNullException.ThrowIfNull(candidates);

        if (!shooter.State.IsAlive)
        {
            return new ShotResult(false, null, HitBox.Chest, 0, 0, false, null);
        }

        // PRD 47 - Mümkün olmayan atəş sürəti.
        double sinceLastShot = (serverTimeMs - shooter.LastShotServerTimeMs) / 1000.0;
        if (ServerValidators.IsImpossibleFireRate(sinceLastShot, _weapon.ShotInterval))
        {
            return new ShotResult(false, null, HitBox.Chest, 0, 0, false, Violation.ImpossibleFireRate);
        }

        shooter.LastShotServerTimeMs = serverTimeMs;
        shooter.ShotsInBurst++;

        // PRD 45 - Hədəfləri atıcının latency-si qədər geriyə sar.
        double rewindTime = LagCompensationBuffer.ResolveRewindTime(serverTimeMs, shooter.LatencyMs);

        Vector3 origin = shooter.Movement.Position
            + new Vector3(0f, HitScan.EyeHeight(shooter.Movement.IsCrouching), 0f);

        Vector3 direction = aimDirection.LengthSquared() > 1e-6f
            ? Vector3.Normalize(aimDirection)
            : -Vector3.UnitZ;

        ServerPlayer? closestVictim = null;
        float closestDistance = float.MaxValue;
        HitBox hitBox = HitBox.Chest;

        // PRD 46 - Hədəf seçimi serverdə aparılır; client-in iddiası qəbul edilmir.
        foreach (ServerPlayer candidate in candidates)
        {
            if (candidate.PeerId == shooter.PeerId || !candidate.State.IsAlive)
            {
                continue;
            }

            PositionRecord? rewound = candidate.History.Rewind(rewindTime);
            Vector3 targetPosition = rewound?.Position ?? candidate.Movement.Position;
            bool crouching = rewound?.IsCrouching ?? candidate.Movement.IsCrouching;

            HitScanResult scan = HitScan.Intersect(origin, direction, targetPosition, crouching);
            if (!scan.Hit || scan.Distance >= closestDistance)
            {
                continue;
            }

            closestDistance = scan.Distance;
            closestVictim = candidate;
            hitBox = scan.HitBox;
        }

        if (closestVictim is null)
        {
            return new ShotResult(true, null, HitBox.Chest, 0, 0, false, null);
        }

        DamageResult damage = DamageRules.Calculate(
            _weapon,
            hitBox,
            closestDistance,
            closestVictim.State.Health,
            closestVictim.State.Armor,
            closestVictim.State.ArmorType);

        return new ShotResult(
            true,
            closestVictim.PeerId,
            hitBox,
            damage.HealthDamage,
            damage.ArmorDamage,
            damage.IsLethal,
            null);
    }
}
