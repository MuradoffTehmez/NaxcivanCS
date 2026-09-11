// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Shared.Config;

/// <summary>
/// PRD 10 - Round və bomba taymerləri.
///
/// <para>
/// PRD 10 açıq şəkildə tələb edir ki, bütün bu dəyərlər server config
/// vasitəsilə dəyişdirilə bilsin. <see cref="GameConstants"/>-dakı rəqəmlər
/// yalnız config yüklənmədikdə istifadə olunan default-lardır.
/// </para>
///
/// <para>
/// Sahələr <c>init</c>-dir və default dəyər daşıyır: JSON-da olmayan açar
/// default-u saxlayır, yarımçıq config bütün taymerləri sıfırlamır.
/// </para>
/// </summary>
public sealed class RoundTimings
{
    public float FreezeTimeSeconds { get; init; } = GameConstants.FreezeTimeSeconds;

    public float BuyTimeSeconds { get; init; } = GameConstants.BuyTimeSeconds;

    public float RoundTimeSeconds { get; init; } = GameConstants.RoundTimeSeconds;

    public float BombTimerSeconds { get; init; } = GameConstants.BombTimerSeconds;

    public float RoundEndDelaySeconds { get; init; } = GameConstants.RoundEndDelaySeconds;

    public float PlantTimeSeconds { get; init; } = GameConstants.PlantTimeSeconds;

    public float DefuseTimeSeconds { get; init; } = GameConstants.DefuseTimeSeconds;

    public float DefuseTimeWithKitSeconds { get; init; } = GameConstants.DefuseTimeWithKitSeconds;

    public float DefuseRadiusMeters { get; init; } = GameConstants.DefuseRadiusMeters;

    public float BombPickupRadiusMeters { get; init; } = GameConstants.BombPickupRadiusMeters;

    /// <summary>Kod default-ları — config faylı olmadıqda istifadə olunur.</summary>
    public static RoundTimings Defaults { get; } = new();

    /// <summary>
    /// Hər dəyər müsbət olmalıdır. Sıfır freeze time round keçidlərini,
    /// sıfır plant müddəti isə anlıq plant-ı yaradardı.
    /// </summary>
    public void Validate()
    {
        foreach ((string name, float value) in Describe())
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new InvalidOperationException(
                    $"Server config: '{name}' müsbət olmalıdır, {value} verilib.");
            }
        }
    }

    private IEnumerable<(string Name, float Value)> Describe()
    {
        yield return (nameof(FreezeTimeSeconds), FreezeTimeSeconds);
        yield return (nameof(BuyTimeSeconds), BuyTimeSeconds);
        yield return (nameof(RoundTimeSeconds), RoundTimeSeconds);
        yield return (nameof(BombTimerSeconds), BombTimerSeconds);
        yield return (nameof(RoundEndDelaySeconds), RoundEndDelaySeconds);
        yield return (nameof(PlantTimeSeconds), PlantTimeSeconds);
        yield return (nameof(DefuseTimeSeconds), DefuseTimeSeconds);
        yield return (nameof(DefuseTimeWithKitSeconds), DefuseTimeWithKitSeconds);
        yield return (nameof(DefuseRadiusMeters), DefuseRadiusMeters);
        yield return (nameof(BombPickupRadiusMeters), BombPickupRadiusMeters);
    }
}

/// <summary>
/// PRD 10, 148, 155 - Serverin JSON konfiqurasiyası.
///
/// Balans dəyişikliyi üçün yenidən build tələb olunmamalıdır, ona görə
/// dəyərlər <c>config/server_default.json</c> faylından oxunur.
/// </summary>
public sealed class ServerConfig
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string ServerName { get; init; } = "NaxcivanCS Dedicated";

    public string Map { get; init; } = "NC_Qala";

    public bool FriendlyFire { get; init; }

    public RoundTimings Round { get; init; } = RoundTimings.Defaults;

    /// <summary>Config faylı tapılmadıqda istifadə olunan tam default konfiqurasiya.</summary>
    public static ServerConfig Defaults { get; } = new();

    /// <summary>
    /// JSON mətnindən oxuyur. Fayl pozuqdursa istisna atılır — səssizcə
    /// default-a qayıtmaq balans dəyişikliyinin tətbiq olunduğu illüziyasını
    /// yaradardı.
    /// </summary>
    public static ServerConfig Parse(string json)
    {
        ServerConfig config = JsonSerializer.Deserialize<ServerConfig>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Server config boşdur.");

        config.Round.Validate();
        return config;
    }

    /// <summary>
    /// Verilmiş yollardan ilk mövcud faylı yükləyir; heç biri yoxdursa
    /// <see cref="Defaults"/> qaytarır.
    /// </summary>
    public static ServerConfig LoadFirstAvailable(IEnumerable<string> candidatePaths)
    {
        ArgumentNullException.ThrowIfNull(candidatePaths);

        foreach (string path in candidatePaths)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return Parse(File.ReadAllText(path));
            }
        }

        return Defaults;
    }
}
