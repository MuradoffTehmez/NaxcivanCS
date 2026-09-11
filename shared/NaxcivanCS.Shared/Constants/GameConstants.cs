// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Constants;

/// <summary>
/// PRD 155 - Balans rəqəmləri kodda hardcode edilməməlidir.
/// Buradakı dəyərlər yalnız server config yüklənmədikdə istifadə olunan
/// default-lardır; runtime-da <c>config/</c> JSON faylları üstün gəlir.
/// </summary>
public static class GameConstants
{
    // ---- PRD 102 - Versioning ----
    // Protocol 3: BombStateChanged mesaji elave olundu (PRD 8).
    // Protocol 4: BuyRequest/BuyResult faktiki olaraq istifade olunur (PRD 27).
    public const int ProtocolVersion = 4;
    public const string GameVersion = "0.4.0";
    public const string ContentVersion = "0.4.0";

    // ---- PRD 9 - Match strukturu (MR12) ----
    public const int PlayersPerTeam = 5;
    public const int MaxPlayers = PlayersPerTeam * 2;
    public const int RegulationRoundsPerHalf = 12;
    public const int RoundsToWin = 13;
    public const int OvertimeRoundsPerHalf = 3;

    // ---- PRD 10 - Round taymerləri (saniyə) ----
    public const float FreezeTimeSeconds = 10f;
    public const float BuyTimeSeconds = 20f;
    public const float RoundTimeSeconds = 105f;   // 1:45
    public const float BombTimerSeconds = 40f;
    public const float RoundEndDelaySeconds = 5f;
    public const float PlantTimeSeconds = 3.2f;
    public const float DefuseTimeSeconds = 10f;
    public const float DefuseTimeWithKitSeconds = 5f;

    /// <summary>Defuse üçün bombaya maksimum məsafə, metr.</summary>
    public const float DefuseRadiusMeters = 1.6f;

    /// <summary>Yerə düşmüş bombanı götürmək üçün maksimum məsafə, metr.</summary>
    public const float BombPickupRadiusMeters = 1.4f;

    /// <summary>
    /// Basılı saxlanan düymənin input paketi gəlmədən neçə ms etibarlı qaldığı.
    /// Unreliable input itkisini örtür, susmuş client-i isə örtmür.
    /// </summary>
    public const double HeldInputGraceMs = 250d;

    // ---- PRD 20 - Health / Armor ----
    public const int MaxHealth = 100;
    public const int MaxArmor = 100;

    // ---- PRD 25 - Economy ----
    public const int StartMoney = 800;
    public const int MaxMoney = 16000;
    public const int RoundWinReward = 3250;
    public const int BombPlantTeamReward = 800;
    public const int BombPlantPlayerReward = 300;
    public const int BombDefusePlayerReward = 300;

    /// <summary>PRD 27 - Defuse kit qiyməti.</summary>
    public const int DefuseKitPrice = 400;
    public const int BombExplodedTeamReward = 3500;

    // ---- PRD 26 - Loss bonus pillələri ----
    public static readonly int[] LossBonusLadder = { 1400, 1900, 2400, 2900, 3400 };

    // ---- PRD 13 - Kamera ----
    public const float DefaultFov = 90f;
    public const float MinFov = 75f;
    public const float MaxFov = 110f;

    // ---- PRD 42, 45 - Network ----
    public const int ServerTickRate = 64;
    public const float ServerTickIntervalSeconds = 1f / ServerTickRate;
    public const int LagCompensationHistoryMs = 200;
    public const int SnapshotInterpolationDelayMs = 100;
    public const int DefaultServerPort = 27015;

    // ---- PRD 48 - Cheat detection scoring ----
    public const int SuspicionReviewThreshold = 80;
    public const int SuspicionRestrictThreshold = 100;

    // ---- PRD 52 - Username ----
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 20;

    // ---- PRD 61 - Party ----
    public const int MaxPartySize = 5;

    // ---- PRD 19 - Hitbox damage multiplier-ləri ----
    public static float HitBoxMultiplier(HitBox hitBox) => hitBox switch
    {
        HitBox.Head => 4.0f,
        HitBox.Chest => 1.0f,
        HitBox.Stomach => 1.2f,
        HitBox.Arms => 1.0f,
        HitBox.Legs => 0.75f,
        _ => 1.0f,
    };

    // ---- PRD 18 - Accuracy (spread) çarpanları ----
    public static float StanceSpreadMultiplier(Stance stance) => stance switch
    {
        Stance.Crouching => 0.7f,   // crouch bonus
        Stance.Standing => 1.0f,    // baseline
        Stance.Walking => 1.6f,
        Stance.Running => 3.0f,
        Stance.Airborne => 8.0f,    // major penalty
        _ => 1.0f,
    };
}
