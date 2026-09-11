// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;

namespace NaxcivanCS.Client.Effects;

/// <summary>
/// PRD 46, 78 - Atəş effektləri: tracer və vurulma izi.
///
/// <para>
/// Effektlər <b>serverin hesabladığı</b> başlanğıc/son nöqtələrdən qurulur.
/// Client özü "hara dəydi" qərarı vermir — sadəcə serverin dediyini göstərir
/// (PRD 156). Beləliklə ekranda gördüyünüz tracer həqiqətən gedən gülləni
/// təmsil edir.
/// </para>
/// </summary>
public sealed partial class ShotEffects : Node3D
{
    private const float TracerSeconds = 0.06f;
    private const float ImpactSeconds = 0.9f;

    /// <summary>Eyni anda göstərilən maksimum effekt — spray zamanı yığılmasın.</summary>
    private const int MaxActiveEffects = 64;

    private readonly List<(Node3D Node, float Remaining, float Lifetime)> _active = new();

    private StandardMaterial3D? _tracerMaterial;
    private StandardMaterial3D? _impactMaterial;

    public override void _Ready()
    {
        _tracerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("ffd88a"),
            EmissionEnabled = true,
            Emission = new Color("ffc457"),
            EmissionEnergyMultiplier = 3.5f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        _impactMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("1a1a1a"),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
    }

    /// <summary>Bir güllə izi çəkir və vurulma nöqtəsində iz qoyur.</summary>
    public void SpawnTracer(Vector3 from, Vector3 to, bool hitPlayer)
    {
        float length = from.DistanceTo(to);
        if (length < 0.1f)
        {
            return;
        }

        var tracer = new MeshInstance3D
        {
            Mesh = new CylinderMesh
            {
                TopRadius = 0.012f,
                BottomRadius = 0.012f,
                Height = length,
                RadialSegments = 4,
                Rings = 0,
            },
            MaterialOverride = _tracerMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        AddChild(tracer);

        // Silindr Y oxu boyuncadır; onu from → to istiqamətinə çevir.
        tracer.GlobalPosition = from.Lerp(to, 0.5f);
        Vector3 direction = (to - from).Normalized();
        if (Mathf.Abs(direction.Dot(Vector3.Up)) < 0.999f)
        {
            tracer.LookAtFromPosition(tracer.GlobalPosition, to, Vector3.Up);
            tracer.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        }

        Track(tracer, TracerSeconds);

        if (!hitPlayer)
        {
            SpawnImpact(to);
        }
    }

    private void SpawnImpact(Vector3 at)
    {
        var impact = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.045f, Height = 0.09f, RadialSegments = 6, Rings = 3 },
            MaterialOverride = _impactMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        AddChild(impact);
        impact.GlobalPosition = at;

        Track(impact, ImpactSeconds);
    }

    private void Track(Node3D node, float lifetime)
    {
        _active.Add((node, lifetime, lifetime));

        while (_active.Count > MaxActiveEffects)
        {
            _active[0].Node.QueueFree();
            _active.RemoveAt(0);
        }
    }

    public override void _Process(double delta)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            (Node3D node, float remaining, float lifetime) = _active[i];
            remaining -= (float)delta;

            if (remaining <= 0f)
            {
                node.QueueFree();
                _active.RemoveAt(i);
                continue;
            }

            // Sönmə effekti.
            if (node is MeshInstance3D mesh)
            {
                float alpha = remaining / lifetime;
                mesh.Transparency = 1f - alpha;
            }

            _active[i] = (node, remaining, lifetime);
        }
    }
}
