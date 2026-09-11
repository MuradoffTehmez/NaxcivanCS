using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Models;

/// <summary>
/// PRD 16 - WeaponData modeli. PRD 155-ə görə hər silah
/// <c>config/weapons/*.json</c> faylından yüklənir.
/// </summary>
public sealed record WeaponData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public WeaponCategory Category { get; init; }

    /// <summary>Baza damage (zirehsiz, chest hit).</summary>
    public int Damage { get; init; }

    /// <summary>0..1 — armor-un damage azaltmasını nə qədər keçir (PRD 20).</summary>
    public float ArmorPenetration { get; init; } = 0.5f;

    /// <summary>Rounds per minute.</summary>
    public int FireRate { get; init; }

    public int MagazineSize { get; init; }
    public int ReserveAmmo { get; init; }
    public float ReloadTime { get; init; }

    /// <summary>Bu silah əlində olarkən hərəkət sürəti çarpanı (PRD 12).</summary>
    public float MovementSpeed { get; init; } = 1.0f;

    // PRD 18 - Accuracy
    public float SpreadStanding { get; init; }
    public float SpreadMoving { get; init; }
    public float SpreadJumping { get; init; }

    /// <summary>PRD 17 - Deterministik recoil pattern (random deyil).</summary>
    public IReadOnlyList<RecoilStep> RecoilPattern { get; init; } = Array.Empty<RecoilStep>();

    /// <summary>Metr — bu məsafədən sonra damage falloff başlayır.</summary>
    public float Range { get; init; } = 30f;

    /// <summary>Range-dən sonra hər metr üçün damage itkisi (0..1 nisbət).</summary>
    public float DamageFalloff { get; init; }

    // PRD 25, 27
    public int Price { get; init; }
    public int KillReward { get; init; } = 300;

    /// <summary>Atışlar arası minimum interval — PRD 47 fire-rate validasiyası üçün.</summary>
    public float ShotInterval => FireRate > 0 ? 60f / FireRate : 0f;
}

/// <summary>PRD 17 - Bir atışın recoil addımı (predefined pattern).</summary>
public readonly record struct RecoilStep(float Vertical, float Horizontal);
