// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NaxcivanCS.Shared.Models;

/// <summary>
/// PRD 11, 43 - Oyunçunun hərəkət vəziyyəti.
/// Həm client prediction, həm də server simulyasiyası eyni strukturu işlədir.
/// Godot tiplərindən asılı deyil — <see cref="Vector3"/> System.Numerics-dəndir.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1051:Görünür örnek alanlarını bildirmeyin",
    Justification = "Hər tick-də mutasiya olunan hot-path struct-dır; property-lər əlavə " +
                    "kopyalama yaradır və `ref` ilə yerində dəyişməyə mane olur.")]
public struct MovementState
{
    public Vector3 Position;
    public Vector3 Velocity;

    /// <summary>Üfüqi baxış bucağı, dərəcə.</summary>
    public float Yaw;

    /// <summary>Şaquli baxış bucağı, dərəcə. [-89, 89] aralığına klamp edilir.</summary>
    public float Pitch;

    public bool IsGrounded;
    public bool IsCrouching;

    /// <summary>Server tərəfdə emal edilmiş son input sequence (PRD 43 reconciliation).</summary>
    public uint LastProcessedSequence;

    public static MovementState AtSpawn(Vector3 position, float yaw) => new()
    {
        Position = position,
        Velocity = Vector3.Zero,
        Yaw = yaw,
        Pitch = 0f,
        IsGrounded = true,
        IsCrouching = false,
    };
}
