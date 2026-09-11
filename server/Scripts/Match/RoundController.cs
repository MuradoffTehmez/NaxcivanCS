using Godot;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;

namespace NaxcivanCS.Server.Match;

/// <summary>
/// PRD 10 - Round state machine: Freeze → Buy → Active → (BombPlanted) → RoundEnd.
/// PRD 156 - Bütün keçidlər yalnız server tərəfdə baş verir.
/// </summary>
public sealed partial class RoundController : Node
{
    private float _phaseTimer;

    [Signal]
    public delegate void PhaseChangedEventHandler(int newPhase);

    public RoundPhase Phase { get; private set; } = RoundPhase.Warmup;

    public int RoundNumber { get; private set; }

    public int AlphaScore { get; private set; }

    public int BravoScore { get; private set; }

    public void StartRound()
    {
        RoundNumber++;
        TransitionTo(RoundPhase.FreezeTime);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Phase is RoundPhase.Warmup)
        {
            return;
        }

        _phaseTimer -= (float)delta;
        if (_phaseTimer > 0f)
        {
            return;
        }

        AdvancePhase();
    }

    /// <summary>PRD 8 - Round nəticəsini qeyd edir və skoru yeniləyir.</summary>
    public void EndRound(Team winner, RoundEndReason reason)
    {
        if (winner == Team.Alpha)
        {
            AlphaScore++;
        }
        else if (winner == Team.Bravo)
        {
            BravoScore++;
        }

        GD.Print($"[Round {RoundNumber}] {winner} qazandı — {reason} ({AlphaScore}:{BravoScore})");
        TransitionTo(RoundPhase.RoundEnd);
    }

    public void OnBombPlanted() => TransitionTo(RoundPhase.BombPlanted);

    public bool IsMatchOver => MatchRules.IsMatchOver(AlphaScore, BravoScore);

    private void AdvancePhase()
    {
        RoundPhase next = Phase switch
        {
            RoundPhase.FreezeTime => RoundPhase.BuyTime,
            RoundPhase.BuyTime => RoundPhase.Active,
            RoundPhase.Active => RoundPhase.RoundEnd,       // PRD 8 - vaxt bitdi, müdafiə qazanır
            RoundPhase.BombPlanted => RoundPhase.RoundEnd,  // PRD 8 - bomba partladı
            RoundPhase.RoundEnd => RoundPhase.FreezeTime,
            _ => Phase,
        };

        if (next == RoundPhase.FreezeTime)
        {
            RoundNumber++;
        }

        TransitionTo(next);
    }

    private void TransitionTo(RoundPhase phase)
    {
        Phase = phase;
        _phaseTimer = MatchRules.PhaseDuration(phase);
        EmitSignal(SignalName.PhaseChanged, (int)phase);
    }
}
