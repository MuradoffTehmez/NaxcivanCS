// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Server.Damage;
using NaxcivanCS.Server.Players;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;
using Xunit;

namespace NaxcivanCS.Server.Tests;

public sealed class ServerGameplayTests
{
    private static readonly WeaponData Rifle = new()
    {
        Id = "test",
        Name = "Test",
        Damage = 34,
        MagazineSize = 30,
        ReserveAmmo = 90,
        FireRate = 600,
    };

    private static ServerPlayer Player(int id = 1, float z = 0) =>
        new(id, $"Player{id}", Team.Alpha, new Vector3(0, 0, z), 0, Rifle);

    private static InputCommand Input(uint sequence) => new(sequence, 0, 0, 0, 0, 0, InputButtons.None);

    [Fact]
    public void InputFlood_KeepsLatest32AndProcessesFourPerTick()
    {
        ServerPlayer player = Player();
        for (uint sequence = 1; sequence <= 40; sequence++)
        {
            player.EnqueueInput(Input(sequence));
        }

        Assert.Equal(new uint[] { 9, 10, 11, 12 }, player.DequeueInputsForTick().Select(i => i.Sequence));
        var remaining = new List<InputCommand>();
        for (int tick = 0; tick < 8; tick++)
        {
            remaining.AddRange(player.DequeueInputsForTick());
        }

        Assert.Equal(28, remaining.Count);
        Assert.Equal(40u, remaining[^1].Sequence);
        Assert.Empty(player.DequeueInputsForTick());
    }

    [Fact]
    public void AcknowledgedAndOlderInput_IsDiscarded()
    {
        ServerPlayer player = Player();
        player.Movement.LastProcessedSequence = 10;
        player.EnqueueInput(Input(9));
        player.EnqueueInput(Input(10));
        player.EnqueueInput(Input(11));
        Assert.Equal(11u, Assert.Single(player.DequeueInputsForTick()).Sequence);
        Assert.Equal(10u, player.LastAcknowledgedSequence);
    }

    [Fact]
    public void LethalDamageAndRespawn_ResetCombatStateAndHistory()
    {
        ServerPlayer player = Player();
        player.State.Armor = 10;
        player.Movement.Velocity = Vector3.One;
        player.History.Record(1000, Vector3.One, 1, true);
        player.ApplyDamage(150, 20, 1000);
        Assert.Equal(0, player.State.Health);
        Assert.Equal(0, player.State.Armor);
        Assert.Equal(1, player.State.Deaths);
        Assert.Equal(Vector3.Zero, player.Movement.Velocity);
        Assert.False(player.ShouldRespawn(3999, 3000));
        Assert.True(player.ShouldRespawn(4000, 3000));

        player.SpawnPosition = new Vector3(2, 0, 3);
        player.SpawnYaw = 1;
        player.Respawn();
        Assert.Equal(GameConstants.MaxHealth, player.State.Health);
        Assert.Equal(player.SpawnPosition, player.Movement.Position);
        Assert.Equal(1f, player.Movement.Yaw);
        Assert.Equal(0, player.History.Count);
        Assert.False(player.ShouldRespawn(10000, 3000));
        PlayerSnapshot snapshot = player.ToSnapshot();
        Assert.Equal(player.PeerId, snapshot.PeerId);
        Assert.Equal(player.Movement.Position, snapshot.Position);
        Assert.Equal(100, snapshot.Health);
    }

    [Fact]
    public void Shot_SelectsClosestLivingTargetAndDoesNotMutateHealth()
    {
        ServerPlayer shooter = Player();
        ServerPlayer near = Player(2, -5);
        ServerPlayer far = Player(3, -10);
        ServerPlayer dead = Player(4, -2);
        dead.State.Health = 0;
        var validator = new HitValidator(Rifle);
        ShotResult shot = validator.Evaluate(shooter, -Vector3.UnitZ, [shooter, far, near, dead], 1000);
        Assert.Equal(near.PeerId, shot.VictimPeerId);
        Assert.Equal(HitBox.Head, shot.HitBox);
        Assert.True(shot.Killed);
        Assert.Equal(100, near.State.Health);
    }

    [Fact]
    public void Shot_UsesRewoundPositionInsteadOfCurrentPosition()
    {
        ServerPlayer shooter = Player();
        shooter.LatencyMs = 100;
        ServerPlayer target = Player(2, -10);
        target.History.Record(900, target.Movement.Position, 0, false);
        target.Movement.Position = new Vector3(20, 0, -10);
        var validator = new HitValidator(Rifle);
        Assert.Equal(2, validator.Evaluate(shooter, -Vector3.UnitZ, [target], 1000).VictimPeerId);
    }

    [Fact]
    public void Miss_ReturnsTracerWithoutDamage()
    {
        var validator = new HitValidator(Rifle);
        ShotResult shot = validator.Evaluate(Player(), Vector3.Zero, [Player(2, 10)], 1000);
        Assert.Null(shot.VictimPeerId);
        Assert.Equal(0, shot.HealthDamage);
        Assert.False(shot.Killed);
        Assert.Equal(120f, Vector3.Distance(shot.Origin, shot.End), 3);
    }

    /// <summary>
    /// Input-lar unreliable gedir: paket itən tick-də düymə buraxılmış
    /// sayılmamalıdır, əks halda plant/defuse heç vaxt tamamlanmır.
    /// </summary>
    [Fact]
    public void HeldButton_SurvivesTicksWithoutInput()
    {
        ServerPlayer player = Player();
        var held = new InputCommand(1, 0, 0, 0, 0, 0, InputButtons.Interact);

        player.RecordProcessedInput(held, 1000d);

        Assert.True(player.IsHolding(InputButtons.Interact, 1000d, GameConstants.HeldInputGraceMs));
        Assert.True(player.IsHolding(InputButtons.Interact, 1200d, GameConstants.HeldInputGraceMs));
    }

    [Fact]
    public void HeldButton_ExpiresWhenClientGoesSilent()
    {
        ServerPlayer player = Player();
        player.RecordProcessedInput(new InputCommand(1, 0, 0, 0, 0, 0, InputButtons.Interact), 1000d);

        Assert.False(player.IsHolding(
            InputButtons.Interact, 1000d + GameConstants.HeldInputGraceMs + 1d, GameConstants.HeldInputGraceMs));
    }

    [Fact]
    public void ReleasingButton_IsObservedOnTheNextInput()
    {
        ServerPlayer player = Player();
        player.RecordProcessedInput(new InputCommand(1, 0, 0, 0, 0, 0, InputButtons.Interact), 1000d);
        player.RecordProcessedInput(new InputCommand(2, 0, 0, 0, 0, 0, InputButtons.None), 1016d);

        Assert.False(player.IsHolding(InputButtons.Interact, 1016d, GameConstants.HeldInputGraceMs));
    }

    [Fact]
    public void HeldButton_IsFalseBeforeAnyInput()
    {
        Assert.False(Player().IsHolding(InputButtons.Interact, 0d, GameConstants.HeldInputGraceMs));
    }
}
