// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

namespace NaxcivanCS.Shared.Enums;

/// <summary>PRD 7 - Iki faction.</summary>
public enum Team
{
    None = 0,
    /// <summary>Hucum eden teref (bomba dasiyir).</summary>
    Alpha = 1,
    /// <summary>Mudafie eden teref (defuse edir).</summary>
    Bravo = 2,
    Spectator = 3,
}

/// <summary>PRD 10 - Round mərhələləri.</summary>
public enum RoundPhase
{
    Warmup = 0,
    FreezeTime = 1,
    BuyTime = 2,
    Active = 3,
    BombPlanted = 4,
    RoundEnd = 5,
}

/// <summary>PRD 9 - Match vəziyyəti.</summary>
public enum MatchState
{
    WaitingForPlayers = 0,
    Warmup = 1,
    Live = 2,
    Halftime = 3,
    Overtime = 4,
    Finished = 5,
    Aborted = 6,
}

/// <summary>PRD 8 - Round nəticə səbəbi.</summary>
public enum RoundEndReason
{
    None = 0,
    AlphaEliminated = 1,
    BravoEliminated = 2,
    BombExploded = 3,
    BombDefused = 4,
    TimeExpired = 5,
}

/// <summary>PRD 8 - Bombanın round daxilindəki vəziyyəti.</summary>
public enum BombState
{
    /// <summary>Hucum eden oyuncunun uzerindedir.</summary>
    Carried = 0,

    /// <summary>Dasiyici olub, bomba yerde qalib.</summary>
    Dropped = 1,

    /// <summary>Site-a yerlesdirilib, taymer isleyir.</summary>
    Planted = 2,

    /// <summary>Mudafie zererzizlesdirdi.</summary>
    Defused = 3,

    /// <summary>Taymer bitdi, bomba partladi.</summary>
    Exploded = 4,
}

/// <summary>PRD 14 - Silah kateqoriyaları (buy menu ilə eyni sıra, PRD 27).</summary>
public enum WeaponCategory
{
    Knife = 0,
    Pistol = 1,
    Smg = 2,
    Rifle = 3,
    Sniper = 4,
    Shotgun = 5,
    Heavy = 6,
    Grenade = 7,
    Equipment = 8,
}

/// <summary>PRD 28 - Inventory slotları.</summary>
public enum InventorySlot
{
    Primary = 0,
    Secondary = 1,
    Knife = 2,
    Grenade = 3,
    Objective = 4,
}

/// <summary>PRD 19 - Hit bölgələri.</summary>
public enum HitBox
{
    Head = 0,
    Chest = 1,
    Stomach = 2,
    Arms = 3,
    Legs = 4,
}

/// <summary>PRD 12, 18 - Accuracy-yə təsir edən stance.</summary>
public enum Stance
{
    Standing = 0,
    Crouching = 1,
    Walking = 2,
    Running = 3,
    Airborne = 4,
}

/// <summary>PRD 20 - Armor vəziyyəti.</summary>
public enum ArmorType
{
    None = 0,
    Vest = 1,
    VestAndHelmet = 2,
}

/// <summary>PRD 22 - Grenade növləri.</summary>
public enum GrenadeType
{
    Fragmentation = 0,
    Flash = 1,
    Smoke = 2,
    Incendiary = 3,
}

/// <summary>PRD 84 - Surface audio materialları.</summary>
public enum SurfaceMaterial
{
    Stone = 0,
    Wood = 1,
    Metal = 2,
    Glass = 3,
    Grass = 4,
    Sand = 5,
    Water = 6,
    Concrete = 7,
}

/// <summary>PRD 8 - Oyun rejimləri.</summary>
public enum GameMode
{
    Competitive = 0,
    Casual = 1,
    Deathmatch = 2,
    Training = 3,
}

/// <summary>PRD 56 - Competitive rank pillələri.</summary>
public enum RankTier
{
    Unranked = 0,
    Recruit = 1,
    BronzeI = 2,
    BronzeII = 3,
    BronzeIII = 4,
    SilverI = 5,
    SilverII = 6,
    SilverIII = 7,
    GoldI = 8,
    GoldII = 9,
    GoldIII = 10,
    Platinum = 11,
    Diamond = 12,
    Elite = 13,
    Master = 14,
    Legend = 15,
}

/// <summary>PRD 49 - Report səbəbləri.</summary>
public enum ReportReason
{
    Cheating = 0,
    Toxicity = 1,
    Griefing = 2,
    Afk = 3,
    Spam = 4,
    AbusiveVoice = 5,
    AbusiveText = 6,
}

/// <summary>PRD 37 - Bot state machine.</summary>
public enum BotState
{
    Idle = 0,
    Patrol = 1,
    Search = 2,
    Engage = 3,
    Retreat = 4,
    Plant = 5,
    Defuse = 6,
}

/// <summary>PRD 38 - Bot çətinlik səviyyələri.</summary>
public enum BotDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Expert = 3,
}

/// <summary>PRD 60 - Regionlar.</summary>
public enum Region
{
    AzCaucasus = 0,
    Turkey = 1,
    CentralEurope = 2,
}
