// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Block-out həndəsəsinin bir bloku.</summary>
/// <param name="Name">Node adı / callout (PRD 33).</param>
/// <param name="Center">Blokun mərkəzi.</param>
/// <param name="Size">Ölçü (en, hündürlük, dərinlik).</param>
/// <param name="Surface">PRD 84 - footstep və penetration üçün material.</param>
public readonly record struct MapBlock(string Name, Vector3 Center, Vector3 Size, SurfaceMaterial Surface);

/// <summary>
/// PRD 127 - Prototype test xəritəsinin block-out həndəsəsi.
///
/// <b>Client və server EYNİ mənbədən qurulur.</b> Əks halda client-in gördüyü
/// örtük serverdə olmaya bilər — oyunçu divarın arxasında gizləndiyini düşünüb
/// vurular. Bu, FPS-lərdə ən pis baq növlərindən biridir, ona görə həndəsə
/// burada, hər iki tərəfin oxuduğu yerdə saxlanılır.
///
/// Həqiqi xəritələr (NC_Qala və s.) Phase 2-də .tscn kimi gələcək.
/// </summary>
public static class BlockoutMap
{
    public const float HalfExtent = 20f;
    public const float WallHeight = 4f;

    /// <summary>Alpha (hücum) tərəfinin spawn xətti — mənfi Z.</summary>
    public const float AlphaSpawnZ = -12f;

    /// <summary>Bravo (müdafiə) tərəfinin spawn xətti — müsbət Z.</summary>
    public const float BravoSpawnZ = 12f;

    public static IReadOnlyList<MapBlock> Blocks { get; } = new MapBlock[]
    {
        new("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), SurfaceMaterial.Concrete),

        // Mərkəzdəki örtüklər — rotasiya və duel məsafələri üçün.
        new("Cover_Mid", new Vector3(0f, 1.4f, 0f), new Vector3(1f, 2.8f, 6f), SurfaceMaterial.Stone),
        new("Cover_A", new Vector3(-6f, 0.9f, -4f), new Vector3(3f, 1.8f, 1f), SurfaceMaterial.Wood),
        new("Cover_B", new Vector3(6f, 0.9f, 4f), new Vector3(3f, 1.8f, 1f), SurfaceMaterial.Wood),
        new("Cover_C", new Vector3(6f, 0.9f, -4f), new Vector3(3f, 1.8f, 1f), SurfaceMaterial.Wood),
        new("Cover_D", new Vector3(-6f, 0.9f, 4f), new Vector3(3f, 1.8f, 1f), SurfaceMaterial.Wood),

        // Hündür qutular — şaquli bucaq və sıçrayış üçün.
        new("Crate_West", new Vector3(-11f, 0.6f, 0f), new Vector3(2f, 1.2f, 2f), SurfaceMaterial.Metal),
        new("Crate_East", new Vector3(11f, 0.6f, 0f), new Vector3(2f, 1.2f, 2f), SurfaceMaterial.Metal),

        // Xarici divarlar.
        new("Wall_North", new Vector3(0f, WallHeight / 2f, -HalfExtent), new Vector3(40f, WallHeight, 1f), SurfaceMaterial.Stone),
        new("Wall_South", new Vector3(0f, WallHeight / 2f, HalfExtent), new Vector3(40f, WallHeight, 1f), SurfaceMaterial.Stone),
        new("Wall_East", new Vector3(HalfExtent, WallHeight / 2f, 0f), new Vector3(1f, WallHeight, 40f), SurfaceMaterial.Stone),
        new("Wall_West", new Vector3(-HalfExtent, WallHeight / 2f, 0f), new Vector3(1f, WallHeight, 40f), SurfaceMaterial.Stone),
    };

    /// <summary>
    /// PRD 84 - Verilmiş nöqtənin ALTINDAKI səthin materialı.
    ///
    /// Addım səsi buna görə seçilir. Oyunçunun ayağı altındakı ən yüksək blok
    /// tapılır; heç nə yoxdursa <see cref="SurfaceMaterial.Concrete"/> qaytarılır.
    /// </summary>
    /// <param name="position">Oyunçunun ayaq nöqtəsi.</param>
    /// <param name="probeDepth">Nə qədər aşağı baxılsın, metr.</param>
    public static SurfaceMaterial SurfaceAt(Vector3 position, float probeDepth = 0.6f)
    {
        SurfaceMaterial result = SurfaceMaterial.Concrete;
        float highestTop = float.NegativeInfinity;

        foreach (MapBlock block in Blocks)
        {
            float halfX = block.Size.X / 2f;
            float halfZ = block.Size.Z / 2f;

            bool insideXz =
                position.X >= block.Center.X - halfX && position.X <= block.Center.X + halfX &&
                position.Z >= block.Center.Z - halfZ && position.Z <= block.Center.Z + halfZ;

            if (!insideXz)
            {
                continue;
            }

            float top = block.Center.Y + (block.Size.Y / 2f);

            // Ayağın altında və çatan məsafədə olan ən yüksək səth.
            if (top <= position.Y + 0.05f && top >= position.Y - probeDepth && top > highestTop)
            {
                highestTop = top;
                result = block.Surface;
            }
        }

        return result;
    }

    /// <summary>Komandanın spawn mövqeyi. <paramref name="index"/> 0-dan başlayır.</summary>
    public static Vector3 SpawnPosition(Team team, int index)
    {
        float x = -4f + (index * 2f);
        float z = team == Team.Alpha ? AlphaSpawnZ : BravoSpawnZ;
        return new Vector3(x, 0.1f, z);
    }

    /// <summary>
    /// Spawn-da baxış bucağı — <b>xəritənin mərkəzinə</b> tərəf.
    /// Godot konvensiyası: yaw 0 = -Z istiqaməti, yaw 180 = +Z.
    /// Alpha mənfi Z-də doğulur, ona görə mərkəzə baxmaq üçün +Z (180°) lazımdır.
    /// </summary>
    public static float SpawnYaw(Team team) => team == Team.Alpha ? 180f : 0f;
}
