// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>
/// PRD 25, 26 - Economy və loss bonus hesablamaları.
/// PRD 156-ya görə bu məntiq YALNIZ server tərəfdə icra olunur;
/// client eyni kodu yalnız UI proqnozu üçün oxuya bilər.
/// </summary>
public static class EconomyRules
{
    /// <summary>PRD 26 - Ardıcıl uduzma sayına görə loss bonus.</summary>
    /// <param name="consecutiveLosses">1 = ilk uduzulan round.</param>
    public static int LossBonus(int consecutiveLosses)
    {
        if (consecutiveLosses <= 0)
        {
            return 0;
        }

        int[] ladder = GameConstants.LossBonusLadder;
        int index = Math.Min(consecutiveLosses, ladder.Length) - 1;
        return ladder[index];
    }

    /// <summary>Round sonu mükafatını hesablayır (kill reward-lar ayrıca verilir).</summary>
    public static int RoundEndReward(bool won, int consecutiveLossesIfLost)
        => won ? GameConstants.RoundWinReward : LossBonus(consecutiveLossesIfLost);

    /// <summary>PRD 25 - Pul əlavəsi, maksimum limitə klamp edilir.</summary>
    public static int AddMoney(int current, int amount)
        => Math.Clamp(current + amount, 0, GameConstants.MaxMoney);

    /// <summary>
    /// PRD 47, 156 - Alışın server tərəfdə validasiyası.
    /// Kifayət qədər pul yoxdursa satınalma rədd edilir.
    /// </summary>
    public static bool TryPurchase(int currentMoney, int price, out int remainingMoney)
    {
        if (price < 0 || currentMoney < price)
        {
            remainingMoney = currentMoney;
            return false;
        }

        remainingMoney = currentMoney - price;
        return true;
    }
}
