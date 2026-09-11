// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>
/// PRD 12, 83 - Addim sesi kadensiyasi.
/// Sessiz yaxinlasma taktiki FPS-in temel mexanikasidir.
/// </summary>
public sealed class FootstepTrackerTests
{
    private const float Frame = 1f / 60f;

    /// <summary>Verilen surete uygun bir kadr ireli aparir.</summary>
    private static bool Step(FootstepTracker tracker, ref Vector3 position, float speed,
        bool grounded = true, bool crouching = false)
    {
        position += new Vector3(speed * Frame, 0f, 0f);
        return tracker.Update(position, Frame, grounded, crouching);
    }

    private static int CountSteps(FootstepTracker tracker, float speed, float seconds,
        bool grounded = true, bool crouching = false)
    {
        var position = Vector3.Zero;
        tracker.Update(position, Frame, grounded, crouching); // ilk cagiris movqeni qeyd edir

        int steps = 0;
        for (int i = 0; i < (int)(seconds / Frame); i++)
        {
            if (Step(tracker, ref position, speed, grounded, crouching))
            {
                steps++;
            }
        }

        return steps;
    }

    [Fact]
    public void Running_ProducesFootstepsAtStrideDistance()
    {
        var tracker = new FootstepTracker();
        int steps = CountSteps(tracker, MovementSimulation.RunSpeed, seconds: 2f);

        // 7.6 m/s * 2 san = 15.2 m; 1.9 m addim -> ~8 addim.
        Assert.InRange(steps, 7, 9);
    }

    [Fact]
    public void Walking_IsSilent()
    {
        // PRD 12 - Shift ile addimlamaq sessiz olmalidir.
        var tracker = new FootstepTracker();
        Assert.Equal(0, CountSteps(tracker, MovementSimulation.WalkSpeed, seconds: 4f));
    }

    [Fact]
    public void Crouching_IsSilentEvenAtRunSpeed()
    {
        var tracker = new FootstepTracker();
        Assert.Equal(0, CountSteps(tracker, MovementSimulation.RunSpeed, seconds: 4f, crouching: true));
    }

    [Fact]
    public void Airborne_ProducesNoFootsteps()
    {
        var tracker = new FootstepTracker();
        Assert.Equal(0, CountSteps(tracker, MovementSimulation.RunSpeed, seconds: 4f, grounded: false));
    }

    [Fact]
    public void StandingStill_ProducesNoFootsteps()
    {
        var tracker = new FootstepTracker();
        Assert.Equal(0, CountSteps(tracker, speed: 0f, seconds: 4f));
    }

    [Fact]
    public void SwitchingFromWalkToRun_DoesNotFireImmediately()
    {
        // Sessiz yaxinlasib qefil qacmaga baslayan oyuncu derhal addim sesi
        // cixarmamalidir - eks halda "addimla yaxinlas, sonra qac" taktikasi
        // hardan gelديyini bildirer.
        var tracker = new FootstepTracker();
        var position = Vector3.Zero;
        tracker.Update(position, Frame, isGrounded: true, isCrouching: false);

        for (int i = 0; i < 120; i++)
        {
            Step(tracker, ref position, MovementSimulation.WalkSpeed);
        }

        // Qacmaga baslayir - ilk bir nece kadrda addim olmamalidir.
        int immediateSteps = 0;
        for (int i = 0; i < 5; i++)
        {
            if (Step(tracker, ref position, MovementSimulation.RunSpeed))
            {
                immediateSteps++;
            }
        }

        Assert.Equal(0, immediateSteps);
    }

    [Fact]
    public void CurrentSpeed_TracksActualMovement()
    {
        var tracker = new FootstepTracker();
        var position = Vector3.Zero;
        tracker.Update(position, Frame, isGrounded: true, isCrouching: false);

        Step(tracker, ref position, 5f);

        Assert.Equal(5f, tracker.CurrentSpeed, 1);
    }

    [Fact]
    public void Reset_ClearsAccumulatedDistance()
    {
        var tracker = new FootstepTracker();
        var position = Vector3.Zero;
        tracker.Update(position, Frame, isGrounded: true, isCrouching: false);

        for (int i = 0; i < 10; i++)
        {
            Step(tracker, ref position, MovementSimulation.RunSpeed);
        }

        tracker.Reset();
        Assert.Equal(0f, tracker.CurrentSpeed);

        // Reset-den sonra ilk cagiris yalniz movqeni qeyd edir.
        Assert.False(tracker.Update(position, Frame, isGrounded: true, isCrouching: false));
    }
}

/// <summary>PRD 84 - Ayaq altindaki sethin materiali.</summary>
public sealed class SurfaceLookupTests
{
    [Fact]
    public void StandingOnFloor_IsConcrete()
    {
        // Floor bloku (0,-0.5,0), olcu 40x1x40 -> ust seth y=0.
        Assert.Equal(SurfaceMaterial.Concrete, BlockoutMap.SurfaceAt(new Vector3(3f, 0.05f, 3f)));
    }

    [Fact]
    public void StandingOnMetalCrate_IsMetal()
    {
        // Crate_West (-11, 0.6, 0), olcu 2x1.2x2 -> ust seth y=1.2.
        Assert.Equal(SurfaceMaterial.Metal, BlockoutMap.SurfaceAt(new Vector3(-11f, 1.22f, 0f)));
    }

    [Fact]
    public void StandingOnWoodenCover_IsWood()
    {
        // Cover_A (-6, 0.9, -4), olcu 3x1.8x1 -> ust seth y=1.8.
        Assert.Equal(SurfaceMaterial.Wood, BlockoutMap.SurfaceAt(new Vector3(-6f, 1.82f, -4f)));
    }

    [Fact]
    public void HigherSurfaceWins_WhenBlocksOverlapInPlan()
    {
        // Qutunun ustunde dayananda dosheme deyil, qutu secilmelidir.
        SurfaceMaterial onCrate = BlockoutMap.SurfaceAt(new Vector3(11f, 1.22f, 0f));
        SurfaceMaterial besideCrate = BlockoutMap.SurfaceAt(new Vector3(14f, 0.05f, 0f));

        Assert.Equal(SurfaceMaterial.Metal, onCrate);
        Assert.Equal(SurfaceMaterial.Concrete, besideCrate);
    }

    [Fact]
    public void FarAboveAnySurface_FallsBackToConcrete()
        => Assert.Equal(SurfaceMaterial.Concrete, BlockoutMap.SurfaceAt(new Vector3(0f, 30f, 0f)));

    [Fact]
    public void EverySpawnPoint_IsOnAKnownSurface()
    {
        foreach (Team team in new[] { Team.Alpha, Team.Bravo })
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 spawn = BlockoutMap.SpawnPosition(team, i);
                Assert.Equal(SurfaceMaterial.Concrete, BlockoutMap.SurfaceAt(spawn));
            }
        }
    }
}
