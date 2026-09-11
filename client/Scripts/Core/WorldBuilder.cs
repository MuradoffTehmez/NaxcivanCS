// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;

namespace NaxcivanCS.Client.Core;

/// <summary>
/// PRD 5, 127, 141 - Block-out xəritəsinin vizual qurulması.
///
/// Həndəsə <see cref="BlockoutMap"/>-dan gəlir (server ilə eyni mənbə).
/// Burada yalnız materiallar və işıq əlavə olunur.
///
/// <para>
/// PRD 5 "Clear Visibility": düşmən oyunçular dekorasiya içində itməməlidir.
/// Ona görə səthlər <b>qəsdən tutqun və az kontrastlı</b>, oyunçular isə
/// parlaq komanda rəngindədir.
/// </para>
/// </summary>
public static class WorldBuilder
{
    // PRD 141 - vizual dil: dark / stone / copper / red accents.
    private static readonly Color FloorDark = new("2b2a28");
    private static readonly Color FloorLight = new("3a3835");
    private static readonly Color StoneColor = new("6d675f");
    private static readonly Color WoodColor = new("8a5f3a");
    private static readonly Color MetalColor = new("5d646c");

    public static void Build(Node3D root)
    {
        ArgumentNullException.ThrowIfNull(root);

        AddLighting(root);

        foreach (MapBlock block in BlockoutMap.Blocks)
        {
            AddBlock(root, block);
        }
    }

    private static void AddLighting(Node3D root)
    {
        root.AddChild(new DirectionalLight3D
        {
            Name = "Sun",
            Rotation = new Vector3(Mathf.DegToRad(-52f), Mathf.DegToRad(38f), 0f),
            LightEnergy = 1.35f,
            ShadowEnabled = true,
        });

        var sky = new ProceduralSkyMaterial
        {
            SkyHorizonColor = new Color("9d9488"),
            GroundHorizonColor = new Color("2b2a28"),
            SkyTopColor = new Color("2f4f78"),
        };

        root.AddChild(new WorldEnvironment
        {
            Name = "Environment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = sky },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightEnergy = 0.95f,

                // Uzaqdakı divarları bir qədər yumşaldır — dərinlik hissi verir.
                // FogSkyAffect = 0: duman yalnız həndəsəyə təsir edir, səmanı
                // yastı boz ləkəyə çevirmir.
                FogEnabled = true,
                FogLightColor = new Color("6e6a64"),
                FogDensity = 0.0025f,
                FogSkyAffect = 0f,
            },
        });
    }

    private static void AddBlock(Node3D root, MapBlock block)
    {
        var size = new Vector3(block.Size.X, block.Size.Y, block.Size.Z);

        var body = new StaticBody3D
        {
            Name = block.Name,
            Position = new Vector3(block.Center.X, block.Center.Y, block.Center.Z),
        };

        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });

        body.AddChild(new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MaterialFor(block, size),
        });

        root.AddChild(body);
    }

    private static StandardMaterial3D MaterialFor(MapBlock block, Vector3 size)
    {
        // Döşəmə şahmat teksturası alır: hərəkət hissini vermək üçün
        // səthdə istinad nöqtələri olmalıdır, yoxsa oyunçu yerindən
        // tərpəndiyini görmür (prototype-da ən çox hiss olunan problem).
        if (block.Name == "Floor")
        {
            return new StandardMaterial3D
            {
                AlbedoTexture = CheckerTexture(FloorDark, FloorLight),
                Uv1Scale = new Vector3(size.X / 2f, size.Z / 2f, 1f),
                Roughness = 0.95f,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            };
        }

        return new StandardMaterial3D
        {
            AlbedoColor = block.Surface switch
            {
                SurfaceMaterial.Wood => WoodColor,
                SurfaceMaterial.Metal => MetalColor,
                _ => StoneColor,
            },
            Roughness = block.Surface == SurfaceMaterial.Metal ? 0.45f : 0.9f,
            Metallic = block.Surface == SurfaceMaterial.Metal ? 0.6f : 0f,
        };
    }

    private static ImageTexture CheckerTexture(Color a, Color b, int size = 64)
    {
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        int half = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool even = (x < half) == (y < half);
                image.SetPixel(x, y, even ? a : b);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>PRD 5 - Oyunçular fonda itməməlidir: parlaq komanda rəngi.</summary>
    public static StandardMaterial3D PlayerMaterial(Team team) => new()
    {
        AlbedoColor = team == Team.Alpha ? new Color("e0623c") : new Color("3f9ad6"),
        Roughness = 0.6f,
    };
}
