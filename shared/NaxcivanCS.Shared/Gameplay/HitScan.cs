using System.Numerics;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Şüa/hitbox kəsişməsinin nəticəsi.</summary>
public readonly record struct HitScanResult(bool Hit, float Distance, HitBox HitBox);

/// <summary>
/// PRD 19, 46 - Hitscan həndəsəsi.
///
/// Godot-dan asılı olmayan saf riyaziyyat — beləliklə serverin hit hesablaması
/// unit testlərlə yoxlanıla bilir (PRD 124). Prototype üçün oyunçu silindrik
/// hitbox ilə təmsil olunur; Phase 2-də sümük əsaslı hitbox-lara keçiləcək.
/// </summary>
public static class HitScan
{
    public const float PlayerRadius = 0.42f;
    public const float StandingHeight = 1.8f;
    public const float CrouchHeight = 1.28f;

    /// <summary>Baş bölgəsinin yuxarıdan payı.</summary>
    private const float HeadFraction = 0.16f;

    /// <summary>Ayaq bölgəsinin aşağıdan payı.</summary>
    private const float LegsFraction = 0.33f;

    /// <summary>Göz səviyyəsi — şüa buradan başlayır.</summary>
    public static float EyeHeight(bool crouching) => (crouching ? CrouchHeight : StandingHeight) - 0.18f;

    /// <summary>
    /// Şüanın oyunçunun silindrik hitbox-u ilə kəsişməsini yoxlayır.
    /// </summary>
    /// <param name="origin">Şüanın başlanğıcı (atıcının gözü).</param>
    /// <param name="direction">Baxış istiqaməti; normallaşdırılmamış ola bilər.</param>
    /// <param name="targetFeet">Hədəfin ayaq nöqtəsi (mövqe).</param>
    /// <param name="crouching">Hədəf çöməlibmi.</param>
    public static HitScanResult Intersect(Vector3 origin, Vector3 direction, Vector3 targetFeet, bool crouching)
    {
        float height = crouching ? CrouchHeight : StandingHeight;

        Vector2 originXz = new(origin.X, origin.Z);
        Vector2 directionXz = new(direction.X, direction.Z);
        Vector2 centerXz = new(targetFeet.X, targetFeet.Z);

        float directionLengthSquared = directionXz.LengthSquared();
        if (directionLengthSquared < 1e-6f)
        {
            return default;
        }

        Vector2 toCenter = centerXz - originXz;
        float projection = Vector2.Dot(toCenter, directionXz) / directionLengthSquared;
        if (projection <= 0f)
        {
            return default; // hədəf arxadadır
        }

        Vector2 closestPoint = originXz + (directionXz * projection);
        if (Vector2.Distance(closestPoint, centerXz) > PlayerRadius)
        {
            return default;
        }

        Vector3 normalizedDirection = Vector3.Normalize(direction);
        float travel = projection * MathF.Sqrt(directionLengthSquared);
        Vector3 hitPoint = origin + (normalizedDirection * travel);
        float relativeHeight = hitPoint.Y - targetFeet.Y;

        if (relativeHeight < 0f || relativeHeight > height)
        {
            return default;
        }

        return new HitScanResult(true, travel, ResolveHitBox(relativeHeight / height));
    }

    /// <summary>PRD 19 - Vurulma hündürlüyünə görə hit bölgəsi.</summary>
    public static HitBox ResolveHitBox(float normalizedHeight) => normalizedHeight switch
    {
        >= 1f - HeadFraction => HitBox.Head,
        <= LegsFraction => HitBox.Legs,
        <= 0.62f => HitBox.Stomach,
        _ => HitBox.Chest,
    };
}
