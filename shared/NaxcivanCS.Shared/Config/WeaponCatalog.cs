// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;
using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Shared.Config;

/// <summary>
/// PRD 155 - Silah balansı build dəyişmədən JSON-dan yüklənir.
/// </summary>
public sealed class WeaponCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Dictionary<string, WeaponData> _weapons = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<WeaponData> All => _weapons.Values;

    public WeaponData? Get(string id) => _weapons.GetValueOrDefault(id);

    public bool Contains(string id) => _weapons.ContainsKey(id);

    public void Add(WeaponData weapon) => _weapons[weapon.Id] = weapon;

    /// <summary>Verilmiş qovluqdakı bütün <c>*.json</c> silah fayllarını yükləyir.</summary>
    public static WeaponCatalog LoadFromDirectory(string directory)
    {
        var catalog = new WeaponCatalog();
        if (!Directory.Exists(directory))
        {
            return catalog;
        }

        foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            WeaponData? weapon = JsonSerializer.Deserialize<WeaponData>(File.ReadAllText(path), SerializerOptions);
            if (weapon is not null)
            {
                catalog.Add(weapon);
            }
        }

        return catalog;
    }

    public static WeaponData? ParseJson(string json)
        => JsonSerializer.Deserialize<WeaponData>(json, SerializerOptions);
}
