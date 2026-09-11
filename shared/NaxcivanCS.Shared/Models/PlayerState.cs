// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Models;

/// <summary>
/// PRD 39, 156 - Oyunçunun server-authoritative vəziyyəti.
/// Client bu strukturu yalnız oxuyur, heç vaxt təyin etmir.
/// </summary>
public sealed class PlayerState
{
    public required int PeerId { get; init; }
    public required string Username { get; init; }
    public Guid UserId { get; init; }

    public Team Team { get; set; } = Team.None;
    public int Health { get; set; } = GameConstantsRef.MaxHealth;
    public int Armor { get; set; }
    public ArmorType ArmorType { get; set; } = ArmorType.None;
    public bool IsAlive => Health > 0;

    public int Money { get; set; } = GameConstantsRef.StartMoney;
    public bool HasDefuseKit { get; set; }
    public bool HasBomb { get; set; }

    public Stance Stance { get; set; } = Stance.Standing;

    // PRD 53, 69 - Match daxili statistika
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Headshots { get; set; }
    public int DamageDealt { get; set; }
    public int Mvps { get; set; }

    // PRD 48 - Anti-cheat
    public int SuspicionScore { get; set; }
}

/// <summary>Constants layer-inə dairəvi asılılıq yaratmamaq üçün kiçik alias.</summary>
internal static class GameConstantsRef
{
    public const int MaxHealth = Constants.GameConstants.MaxHealth;
    public const int StartMoney = Constants.GameConstants.StartMoney;
}
