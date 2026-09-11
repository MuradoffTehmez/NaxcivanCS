// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Server.Players;

/// <summary>
/// PRD 39, 156 - Bir oyunçunun serverdəki authoritative vəziyyəti.
///
/// Client bu obyektin heç bir sahəsini birbaşa təyin edə bilmir; yeganə giriş
/// nöqtəsi <see cref="EnqueueInput"/> metodudur (PRD 46).
/// </summary>
public sealed class ServerPlayer
{
    private readonly Queue<InputCommand> _pendingInputs = new();

    /// <summary>Bir tick-də emal ediləcək maksimum input — flood müdafiəsi (PRD 47).</summary>
    private const int MaxInputsPerTick = 4;

    /// <summary>Buferdə saxlanan maksimum input sayı.</summary>
    private const int MaxQueuedInputs = 32;

    public ServerPlayer(int peerId, string username, Team team, Vector3 spawnPosition, float spawnYaw, WeaponData weapon)
    {
        PeerId = peerId;
        State = new PlayerState { PeerId = peerId, Username = username, Team = team };
        Movement = MovementState.AtSpawn(spawnPosition, spawnYaw);
        SpawnPosition = spawnPosition;
        SpawnYaw = spawnYaw;
        Weapon = new WeaponRuntime(weapon);
    }

    public int PeerId { get; }

    public PlayerState State { get; }

    /// <summary>
    /// Hər tick-də yerində mutasiya olunur; property olsaydı hər girişdə
    /// struct kopyalanardı və <c>Movement.Position = ...</c> işləməzdi.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1051:Görünür örnek alanlarını bildirmeyin",
        Justification = "Hot-path mutable struct — yerində dəyişdirilməlidir.")]
    public MovementState Movement;

    public LagCompensationBuffer History { get; } = new();

    public Vector3 SpawnPosition { get; set; }

    public float SpawnYaw { get; set; }

    /// <summary>
    /// PRD 14, 17 - Silahın server tərəfdəki vəziyyəti: patron, reload, recoil.
    /// Atəş kadensiyasını bu obyekt təyin edir, client yox (PRD 156).
    /// </summary>
    public WeaponRuntime Weapon { get; }

    /// <summary>Ölüm vaxtı; respawn taymeri bundan hesablanır.</summary>
    public double DeathServerTimeMs { get; private set; }

    /// <summary>Son ölçülən gediş-gəliş latency-si, ms (PRD 45).</summary>
    public double LatencyMs { get; set; }

    public uint LastAcknowledgedSequence => Movement.LastProcessedSequence;

    /// <summary>PRD 46 - Client-dən gələn yeganə gameplay girişi.</summary>
    public void EnqueueInput(InputCommand input)
    {
        // Köhnə və ya təkrar sequence-ləri at (paket sırası UDP-də zəmanətli deyil).
        if (input.Sequence <= Movement.LastProcessedSequence)
        {
            return;
        }

        if (_pendingInputs.Count >= MaxQueuedInputs)
        {
            _pendingInputs.Dequeue();
        }

        _pendingInputs.Enqueue(input);
    }

    /// <summary>
    /// Bir server tick-i üçün növbədəki input-ları qaytarır.
    /// Bir tick-də emal oluna biləcək say məhduddur ki, input flood-u
    /// oyunçuya sürət üstünlüyü verməsin (PRD 47).
    /// </summary>
    public IEnumerable<InputCommand> DequeueInputsForTick()
    {
        int budget = MaxInputsPerTick;
        while (budget-- > 0 && _pendingInputs.Count > 0)
        {
            yield return _pendingInputs.Dequeue();
        }
    }

    public void ApplyDamage(int healthDamage, int armorDamage, double serverTimeMs)
    {
        State.Armor = Math.Max(0, State.Armor - armorDamage);
        State.Health = Math.Max(0, State.Health - healthDamage);

        if (State.Health == 0)
        {
            State.Deaths++;
            DeathServerTimeMs = serverTimeMs;
            Movement.Velocity = Vector3.Zero;
        }
    }

    /// <summary>PRD 127 - Prototype-da sadə respawn; Phase 3-də round sisteminə bağlanacaq.</summary>
    public bool ShouldRespawn(double serverTimeMs, double respawnDelayMs)
        => !State.IsAlive && serverTimeMs - DeathServerTimeMs >= respawnDelayMs;

    public void Respawn()
    {
        State.Health = GameConstants.MaxHealth;
        State.Armor = 0;
        State.ArmorType = ArmorType.None;
        Movement = MovementState.AtSpawn(SpawnPosition, SpawnYaw);
        History.Clear();
        Weapon.RefillOnRespawn();
    }

    public PlayerSnapshot ToSnapshot() => new(
        PeerId,
        Movement.Position,
        Movement.Yaw,
        Movement.Pitch,
        State.Health,
        State.Armor,
        Movement.IsCrouching,
        State.Team,
        Movement.LastProcessedSequence);
}
