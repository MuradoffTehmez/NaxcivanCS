// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>
/// PRD 11, 12, 43 - Movement testleri.
/// Bu kod client prediction ve server authority arasinda paylasilir,
/// ona gore deterministik olmasi kritikdir.
/// </summary>
public sealed class MovementSimulationTests
{
    private const float Tick = GameConstants.ServerTickIntervalSeconds;

    private static InputCommand Input(
        float forward = 0f,
        float right = 0f,
        float yaw = 0f,
        float pitch = 0f,
        InputButtons buttons = InputButtons.None,
        uint sequence = 1)
        => new(sequence, 0, forward, right, yaw, pitch, buttons);

    [Fact]
    public void Step_IsDeterministic_ForIdenticalInputs()
    {
        MovementState a = MovementState.AtSpawn(Vector3.Zero, 0f);
        MovementState b = MovementState.AtSpawn(Vector3.Zero, 0f);

        for (uint i = 1; i <= 64; i++)
        {
            InputCommand input = Input(forward: 1f, right: 0.5f, yaw: 42f, sequence: i);
            a = MovementSimulation.Step(a, input, Tick);
            b = MovementSimulation.Step(b, input, Tick);
        }

        // Eyni giris -> bit-be-bit eyni netice. Reconciliation bundan asilidir.
        Assert.Equal(a.Velocity, b.Velocity);
        Assert.Equal(a.Yaw, b.Yaw);
        Assert.Equal(a.LastProcessedSequence, b.LastProcessedSequence);
    }

    [Fact]
    public void ForwardInput_AcceleratesTowardRunSpeed()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        for (uint i = 1; i <= 64; i++)
        {
            state = MovementSimulation.Step(state, Input(forward: 1f, sequence: i), Tick);
        }

        float speed = MovementSimulation.HorizontalSpeed(state);
        Assert.InRange(speed, MovementSimulation.RunSpeed * 0.9f, MovementSimulation.RunSpeed * 1.01f);
    }

    [Fact]
    public void WalkModifier_CapsSpeedBelowRun()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        for (uint i = 1; i <= 64; i++)
        {
            state = MovementSimulation.Step(state, Input(forward: 1f, buttons: InputButtons.Walk, sequence: i), Tick);
        }

        Assert.True(MovementSimulation.HorizontalSpeed(state) <= MovementSimulation.WalkSpeed + 0.05f);
    }

    [Fact]
    public void Crouching_IsSlowerThanWalking()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        for (uint i = 1; i <= 64; i++)
        {
            state = MovementSimulation.Step(state, Input(forward: 1f, buttons: InputButtons.Crouch, sequence: i), Tick);
        }

        Assert.True(state.IsCrouching);
        Assert.True(MovementSimulation.HorizontalSpeed(state) <= MovementSimulation.CrouchSpeed + 0.05f);
    }

    [Fact]
    public void NoInput_ComesToRest()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        for (uint i = 1; i <= 32; i++)
        {
            state = MovementSimulation.Step(state, Input(forward: 1f, sequence: i), Tick);
        }

        for (uint i = 33; i <= 96; i++)
        {
            state = MovementSimulation.Step(state, Input(sequence: i), Tick);
        }

        Assert.True(MovementSimulation.HorizontalSpeed(state) < 0.01f);
    }

    [Fact]
    public void Jump_LeavesTheGroundAndGravityPullsBack()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        state = MovementSimulation.Step(state, Input(buttons: InputButtons.Jump, sequence: 1), Tick);
        Assert.False(state.IsGrounded);
        Assert.True(state.Velocity.Y > 0f);

        float afterJump = state.Velocity.Y;
        state = MovementSimulation.Step(state, Input(sequence: 2), Tick);

        Assert.True(state.Velocity.Y < afterJump);
    }

    [Fact]
    public void Pitch_IsClampedToLookLimits()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);

        state = MovementSimulation.Step(state, Input(pitch: 180f, sequence: 1), Tick);
        Assert.Equal(MovementSimulation.MaxPitchDegrees, state.Pitch);

        state = MovementSimulation.Step(state, Input(pitch: -180f, sequence: 2), Tick);
        Assert.Equal(-MovementSimulation.MaxPitchDegrees, state.Pitch);
    }

    [Fact]
    public void Yaw_IsNormalizedIntoZeroToThreeSixty()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);
        state = MovementSimulation.Step(state, Input(yaw: -90f, sequence: 1), Tick);

        Assert.Equal(270f, state.Yaw, 2);
    }

    [Fact]
    public void AirControl_IsWeakerThanGroundControl()
    {
        MovementState ground = MovementState.AtSpawn(Vector3.Zero, 0f);
        ground = MovementSimulation.Step(ground, Input(forward: 1f, sequence: 1), Tick);

        MovementState air = MovementState.AtSpawn(Vector3.Zero, 0f);
        air.IsGrounded = false;
        air = MovementSimulation.Step(air, Input(forward: 1f, sequence: 1), Tick);

        Assert.True(MovementSimulation.HorizontalSpeed(air) < MovementSimulation.HorizontalSpeed(ground));
    }

    [Fact]
    public void Stance_ReflectsMovementForAccuracyPenalty()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);
        Assert.Equal(Stance.Standing, MovementSimulation.ResolveStance(state, walkHeld: false));

        for (uint i = 1; i <= 64; i++)
        {
            state = MovementSimulation.Step(state, Input(forward: 1f, sequence: i), Tick);
        }

        Assert.Equal(Stance.Running, MovementSimulation.ResolveStance(state, walkHeld: false));

        state.IsGrounded = false;
        Assert.Equal(Stance.Airborne, MovementSimulation.ResolveStance(state, walkHeld: false));
    }

    [Fact]
    public void Reconciliation_TriggersOnlyBeyondThreshold()
    {
        var predicted = new Vector3(0f, 0f, 0f);

        Assert.False(MovementSimulation.NeedsReconciliation(predicted, new Vector3(0.01f, 0f, 0f)));
        Assert.True(MovementSimulation.NeedsReconciliation(predicted, new Vector3(0.5f, 0f, 0f)));
    }

    [Fact]
    public void LastProcessedSequence_TracksTheInput()
    {
        MovementState state = MovementState.AtSpawn(Vector3.Zero, 0f);
        state = MovementSimulation.Step(state, Input(sequence: 77), Tick);

        Assert.Equal(77u, state.LastProcessedSequence);
    }
}
