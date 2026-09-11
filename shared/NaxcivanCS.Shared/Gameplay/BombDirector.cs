// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Bir tick-də bomba ilə bağlı baş verən ən əhəmiyyətli hadisə.</summary>
public enum BombEvent
{
    None = 0,

    /// <summary>Yerə düşmüş bomba götürüldü.</summary>
    PickedUp = 1,

    /// <summary>Daşıyıcı öldü, bomba yerə düşdü.</summary>
    Dropped = 2,

    /// <summary>Plant başladı və ya davam edir.</summary>
    PlantProgressed = 3,

    /// <summary>Plant yarımçıq dayandı.</summary>
    PlantCancelled = 4,

    /// <summary>Bomba yerləşdirildi — round taymeri bomba taymeri ilə əvəz olunur.</summary>
    Planted = 5,

    /// <summary>Defuse başladı və ya davam edir.</summary>
    DefuseProgressed = 6,

    /// <summary>Defuse yarımçıq dayandı.</summary>
    DefuseCancelled = 7,

    /// <summary>Bomba zərərsizləşdirildi.</summary>
    Defused = 8,

    /// <summary>Taymer bitdi.</summary>
    Exploded = 9,
}

/// <summary>
/// PRD 8 - Bir oyunçunun bu tick-dəki bomba ilə əlaqəli niyyəti.
/// </summary>
/// <param name="PeerId">Oyunçunun peer id-si.</param>
/// <param name="Team">Komanda; yalnız Alpha plant, yalnız Bravo defuse edir.</param>
/// <param name="IsAlive">Ölü oyunçu nə plant, nə defuse edə bilər.</param>
/// <param name="Position">Ayaq mövqeyi.</param>
/// <param name="IsInteracting">Interact düyməsi basılı saxlanılıb?</param>
/// <param name="HasDefuseKit">PRD 8 - kit defuse müddətini yarıya endirir.</param>
public readonly record struct BombInteractor(
    int PeerId,
    Team Team,
    bool IsAlive,
    Vector3 Position,
    bool IsInteracting,
    bool HasDefuseKit);

/// <summary>
/// PRD 8 - Bomba objective-inin idarəçisi.
///
/// <para>
/// <see cref="MatchDirector"/> kimi saf state machine: Godot-a, şəbəkəyə və ya
/// oyunçu obyektlərinə istinad etmir. Girişi yalnız tick müddəti, round fazası
/// və oyunçuların mövqe/düymə vəziyyətidir. Bu, plant/defuse qaydalarının
/// tam unit testlə yoxlanmasına imkan verir.
/// </para>
///
/// <para>
/// Vəziyyət axını:
/// <c>Carried → (daşıyıcı ölür) Dropped → (götürülür) Carried → Planted →
/// Defused | Exploded</c>
/// </para>
///
/// <para>
/// Bomba taymerinin özü burada saxlanılmır — yerləşdirildikdən sonra
/// <see cref="RoundPhase.BombPlanted"/> fazasının müddəti onu idarə edir, yəni
/// tək bir taymer mənbəyi olur.
/// </para>
/// </summary>
public sealed class BombDirector
{
    private readonly float _plantSeconds;
    private readonly float _defuseSeconds;
    private readonly float _defuseWithKitSeconds;
    private readonly float _defuseRadius;
    private readonly float _pickupRadius;

    public BombDirector(
        float plantSeconds = GameConstants.PlantTimeSeconds,
        float defuseSeconds = GameConstants.DefuseTimeSeconds,
        float defuseWithKitSeconds = GameConstants.DefuseTimeWithKitSeconds,
        float defuseRadius = GameConstants.DefuseRadiusMeters,
        float pickupRadius = GameConstants.BombPickupRadiusMeters)
    {
        _plantSeconds = plantSeconds;
        _defuseSeconds = defuseSeconds;
        _defuseWithKitSeconds = defuseWithKitSeconds;
        _defuseRadius = defuseRadius;
        _pickupRadius = pickupRadius;
    }

    public BombState State { get; private set; } = BombState.Carried;

    /// <summary>Bombanı daşıyan oyunçu; daşınmırsa 0.</summary>
    public int CarrierPeerId { get; private set; }

    /// <summary>Yerə düşmüş və ya yerləşdirilmiş bombanın mövqeyi.</summary>
    public Vector3 Position { get; private set; }

    /// <summary>Yerləşdirildiyi site adı ("A" / "B"); yerləşdirilməyibsə boş.</summary>
    public string PlantedSite { get; private set; } = string.Empty;

    /// <summary>Hazırda plant və ya defuse edən oyunçu; heç kim yoxdursa 0.</summary>
    public int InteractingPeerId { get; private set; }

    /// <summary>Plant tamamlanma nisbəti, 0..1.</summary>
    public float PlantProgress { get; private set; }

    /// <summary>Defuse tamamlanma nisbəti, 0..1.</summary>
    public float DefuseProgress { get; private set; }

    /// <summary>Bombanı zərərsizləşdirən oyunçu; yoxdursa 0.</summary>
    public int DefuserPeerId { get; private set; }

    /// <summary>Bombanı yerləşdirən oyunçu; yoxdursa 0.</summary>
    public int PlanterPeerId { get; private set; }

    /// <summary>PRD 8 - Yeni round: bomba hücum edən oyunçuya verilir.</summary>
    public void BeginRound(int carrierPeerId, Vector3 carrierPosition)
    {
        State = BombState.Carried;
        CarrierPeerId = carrierPeerId;
        Position = carrierPosition;
        PlantedSite = string.Empty;
        InteractingPeerId = 0;
        PlantProgress = 0f;
        DefuseProgress = 0f;
        DefuserPeerId = 0;
        PlanterPeerId = 0;
    }

    /// <summary>
    /// Daşıyıcı öldü — bomba göstərilən nöqtəyə düşür.
    /// Yerləşdirilmiş bombaya təsir etmir.
    /// </summary>
    public BombEvent OnCarrierDied(Vector3 dropPosition)
    {
        if (State != BombState.Carried)
        {
            return BombEvent.None;
        }

        State = BombState.Dropped;
        CarrierPeerId = 0;
        Position = dropPosition;
        CancelPlant();
        return BombEvent.Dropped;
    }

    /// <summary>
    /// Bir server tick-i.
    /// </summary>
    /// <param name="delta">Tick müddəti, saniyə.</param>
    /// <param name="phase">Cari round fazası.</param>
    /// <param name="interactors">Bu tick-dəki bütün oyunçular.</param>
    public BombEvent Tick(float delta, RoundPhase phase, IReadOnlyList<BombInteractor> interactors)
    {
        ArgumentNullException.ThrowIfNull(interactors);

        return State switch
        {
            BombState.Carried or BombState.Dropped => TickCarriedOrDropped(delta, phase, interactors),
            BombState.Planted => TickPlanted(delta, interactors),
            _ => BombEvent.None,
        };
    }

    /// <summary>PRD 10 - Bomba taymeri bitdi; round fazası bunu bildirir.</summary>
    public BombEvent OnTimerExpired()
    {
        if (State != BombState.Planted)
        {
            return BombEvent.None;
        }

        State = BombState.Exploded;
        CancelDefuse();
        return BombEvent.Exploded;
    }

    private BombEvent TickCarriedOrDropped(
        float delta, RoundPhase phase, IReadOnlyList<BombInteractor> interactors)
    {
        // Yerdəki bomba götürülə bilər — bu, aktiv olmayan fazada da mümkündür,
        // çünki freeze/buy zamanı daşıyıcı dəyişməsi oyuna zərər vermir.
        if (State == BombState.Dropped && TryPickUp(interactors) is { } picked)
        {
            return picked;
        }

        // PRD 10 - Plant yalnız aktiv roundda mümkündür.
        if (phase != RoundPhase.Active || State != BombState.Carried)
        {
            return CancelPlantIfRunning();
        }

        BombInteractor? carrier = FindInteractor(interactors, CarrierPeerId);

        if (carrier is not { } holder || !holder.IsAlive || !holder.IsInteracting)
        {
            return CancelPlantIfRunning();
        }

        string? site = BlockoutMap.SiteAt(holder.Position);

        if (site is null)
        {
            return CancelPlantIfRunning();
        }

        Position = holder.Position;
        InteractingPeerId = holder.PeerId;
        PlantProgress = Math.Min(1f, PlantProgress + (delta / _plantSeconds));

        if (PlantProgress < 1f)
        {
            return BombEvent.PlantProgressed;
        }

        State = BombState.Planted;
        PlantedSite = site;
        PlanterPeerId = holder.PeerId;
        CarrierPeerId = 0;
        InteractingPeerId = 0;
        PlantProgress = 1f;
        return BombEvent.Planted;
    }

    private BombEvent TickPlanted(float delta, IReadOnlyList<BombInteractor> interactors)
    {
        BombInteractor? defuser = FindDefuser(interactors);

        if (defuser is not { } active)
        {
            return CancelDefuseIfRunning();
        }

        // Defuse-u başqa oyunçu davam etdirirsə irəliləyiş sıfırlanır:
        // yarımçıq defuse komandalar arasında paylaşıla bilməz.
        if (InteractingPeerId != active.PeerId)
        {
            InteractingPeerId = active.PeerId;
            DefuseProgress = 0f;
        }

        float required = active.HasDefuseKit ? _defuseWithKitSeconds : _defuseSeconds;
        DefuseProgress = Math.Min(1f, DefuseProgress + (delta / required));

        if (DefuseProgress < 1f)
        {
            return BombEvent.DefuseProgressed;
        }

        State = BombState.Defused;
        DefuserPeerId = active.PeerId;
        InteractingPeerId = 0;
        return BombEvent.Defused;
    }

    private BombEvent? TryPickUp(IReadOnlyList<BombInteractor> interactors)
    {
        foreach (BombInteractor candidate in interactors)
        {
            if (candidate.Team != Team.Alpha || !candidate.IsAlive)
            {
                continue;
            }

            if (Vector3.Distance(candidate.Position, Position) > _pickupRadius)
            {
                continue;
            }

            State = BombState.Carried;
            CarrierPeerId = candidate.PeerId;
            Position = candidate.Position;
            return BombEvent.PickedUp;
        }

        return null;
    }

    /// <summary>Bombanın yanında, sağ və interact saxlayan ilk müdafiəçi.</summary>
    private BombInteractor? FindDefuser(IReadOnlyList<BombInteractor> interactors)
    {
        foreach (BombInteractor candidate in interactors)
        {
            if (candidate.Team != Team.Bravo || !candidate.IsAlive || !candidate.IsInteracting)
            {
                continue;
            }

            if (Vector3.Distance(candidate.Position, Position) <= _defuseRadius)
            {
                return candidate;
            }
        }

        return null;
    }

    private static BombInteractor? FindInteractor(IReadOnlyList<BombInteractor> interactors, int peerId)
    {
        foreach (BombInteractor candidate in interactors)
        {
            if (candidate.PeerId == peerId)
            {
                return candidate;
            }
        }

        return null;
    }

    private BombEvent CancelPlantIfRunning()
    {
        if (PlantProgress <= 0f)
        {
            return BombEvent.None;
        }

        CancelPlant();
        return BombEvent.PlantCancelled;
    }

    private void CancelPlant()
    {
        PlantProgress = 0f;
        InteractingPeerId = 0;
    }

    private BombEvent CancelDefuseIfRunning()
    {
        if (DefuseProgress <= 0f)
        {
            return BombEvent.None;
        }

        CancelDefuse();
        return BombEvent.DefuseCancelled;
    }

    private void CancelDefuse()
    {
        DefuseProgress = 0f;
        InteractingPeerId = 0;
    }
}
