using System.Numerics;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Net;

/// <summary>
/// PRD 44 - Bir oyunçunun bir tick-dəki şəkli. Server yaradır, client interpolyasiya edir.
/// </summary>
/// <param name="LastProcessedSequence">
/// PRD 43 - Serverin bu oyunçu üçün emal etdiyi son input sequence.
/// Client bundan istifadə edərək təsdiqlənmiş input-ları tarixçədən silir və
/// reconciliation zamanı yalnız qalanları yenidən oynadır.
/// </param>
public readonly record struct PlayerSnapshot(
    int PeerId,
    Vector3 Position,
    float Yaw,
    float Pitch,
    int Health,
    int Armor,
    bool IsCrouching,
    Team Team,
    uint LastProcessedSequence);

/// <summary>PRD 44 - Serverin bir tick-də yaydığı tam dünya vəziyyəti.</summary>
public sealed record WorldSnapshot(
    uint Tick,
    double ServerTimeMs,
    IReadOnlyList<PlayerSnapshot> Players);
