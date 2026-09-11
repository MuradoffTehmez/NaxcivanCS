// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>PRD 9 - MR12 match strukturu və overtime.</summary>
public static class MatchRules
{
    /// <summary>Yarı vaxtın (side swap) baş verdiyi round nömrəsi.</summary>
    public const int HalftimeAfterRound = GameConstants.RegulationRoundsPerHalf;

    /// <summary>Match bitibsə qalib komandanı qaytarır, əks halda <see cref="Team.None"/>.</summary>
    public static Team GetWinner(int alphaScore, int bravoScore)
    {
        if (alphaScore >= GameConstants.RoundsToWin && alphaScore - bravoScore >= 1)
        {
            return alphaScore > bravoScore ? Team.Alpha : Team.None;
        }

        if (bravoScore >= GameConstants.RoundsToWin && bravoScore - alphaScore >= 1)
        {
            return bravoScore > alphaScore ? Team.Bravo : Team.None;
        }

        return Team.None;
    }

    /// <summary>PRD 9 - 12:12 olduqda overtime tətbiq edilir.</summary>
    public static bool RequiresOvertime(int alphaScore, int bravoScore)
        => alphaScore == GameConstants.RegulationRoundsPerHalf
           && bravoScore == GameConstants.RegulationRoundsPerHalf;

    public static bool IsMatchOver(int alphaScore, int bravoScore)
        => GetWinner(alphaScore, bravoScore) != Team.None;

    /// <summary>Növbəti roundda tərəflər dəyişməlidirmi?</summary>
    public static bool IsSideSwapRound(int completedRounds)
        => completedRounds == HalftimeAfterRound;

    /// <summary>PRD 10 - Round fazasının kod default-u ilə müddəti.</summary>
    public static float PhaseDuration(RoundPhase phase)
        => PhaseDuration(phase, RoundTimings.Defaults);

    /// <summary>PRD 10 - Round fazasının server config-dən gələn müddəti.</summary>
    public static float PhaseDuration(RoundPhase phase, RoundTimings timings)
    {
        ArgumentNullException.ThrowIfNull(timings);

        return phase switch
        {
            RoundPhase.FreezeTime => timings.FreezeTimeSeconds,
            RoundPhase.BuyTime => timings.BuyTimeSeconds,
            RoundPhase.Active => timings.RoundTimeSeconds,
            RoundPhase.BombPlanted => timings.BombTimerSeconds,
            RoundPhase.RoundEnd => timings.RoundEndDelaySeconds,
            _ => 0f,
        };
    }
}
