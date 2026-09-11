// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Server.Damage;

/// <summary>Serverin hesabladığı atəş nəticəsi.</summary>
public readonly record struct ShotResult(
    int? VictimPeerId,
    HitBox HitBox,
    int HealthDamage,
    int ArmorDamage,
    bool Killed,
    Vector3 Origin,
    Vector3 End);

/// <summary>
/// PRD 45, 46 - Atəşin serverdə hesablanması.
///
/// <para>
/// Client YALNIZ "tetik basılıdır + bu istiqamətə baxıram" deyir. Neçə güllə
/// çıxdığını <see cref="WeaponRuntime"/>, kimin vurulduğunu isə bu sinif
/// hesablayır.
/// </para>
///
/// <para>
/// <b>Fire-rate validasiyası burada yoxdur və olmamalıdır.</b> Kadensiya
/// server tərəfdə struktur olaraq təmin olunur — client atəş sürətini
/// dəyişdirə bilmir, ona görə aşkarlamağa ehtiyac qalmır. Əvvəlki dizayn
/// hər input paketini atış cəhdi sayırdı və qanuni oyunçulara saniyədə
/// onlarla yalançı pozuntu yazırdı (PRD 47, 48).
/// </para>
/// </summary>
public sealed class HitValidator
{
    /// <summary>Güllənin maksimum uçuş məsafəsi (tracer üçün son nöqtə).</summary>
    private const float MaxTraceDistance = 120f;

    private readonly WeaponData _weapon;

    public HitValidator(WeaponData weapon) => _weapon = weapon;

    /// <summary>
    /// Bir güllənin nəticəsini hesablayır.
    /// </summary>
    /// <param name="shooter">Atıcı.</param>
    /// <param name="direction">Recoil və spread artıq tətbiq olunmuş istiqamət.</param>
    /// <param name="candidates">Potensial hədəflər.</param>
    /// <param name="serverTimeMs">Cari server vaxtı.</param>
    public ShotResult Evaluate(
        ServerPlayer shooter,
        Vector3 direction,
        IEnumerable<ServerPlayer> candidates,
        double serverTimeMs)
    {
        ArgumentNullException.ThrowIfNull(shooter);
        ArgumentNullException.ThrowIfNull(candidates);

        Vector3 origin = shooter.Movement.Position
            + new Vector3(0f, HitScan.EyeHeight(shooter.Movement.IsCrouching), 0f);

        Vector3 normalized = direction.LengthSquared() > 1e-6f
            ? Vector3.Normalize(direction)
            : -Vector3.UnitZ;

        // PRD 45 - Hədəfləri atıcının latency-si qədər geriyə sar.
        double rewindTime = LagCompensationBuffer.ResolveRewindTime(serverTimeMs, shooter.LatencyMs);

        ServerPlayer? closestVictim = null;
        float closestDistance = float.MaxValue;
        HitBox hitBox = HitBox.Chest;

        foreach (ServerPlayer candidate in candidates)
        {
            if (candidate.PeerId == shooter.PeerId || !candidate.State.IsAlive)
            {
                continue;
            }

            PositionRecord? rewound = candidate.History.Rewind(rewindTime);
            Vector3 targetPosition = rewound?.Position ?? candidate.Movement.Position;
            bool crouching = rewound?.IsCrouching ?? candidate.Movement.IsCrouching;

            HitScanResult scan = HitScan.Intersect(origin, normalized, targetPosition, crouching);
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
            return new ShotResult(
                null, HitBox.Chest, 0, 0, false, origin, origin + (normalized * MaxTraceDistance));
        }

        DamageResult damage = DamageRules.Calculate(
            _weapon,
            hitBox,
            closestDistance,
            closestVictim.State.Health,
            closestVictim.State.Armor,
            closestVictim.State.ArmorType);

        return new ShotResult(
            closestVictim.PeerId,
            hitBox,
            damage.HealthDamage,
            damage.ArmorDamage,
            damage.IsLethal,
            origin,
            origin + (normalized * closestDistance));
    }
}
