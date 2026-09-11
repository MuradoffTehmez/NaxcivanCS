// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>
/// PRD 27 - Alış qaydaları.
///
/// <para>
/// Saf funksiyalar: server qərarı burada verilir, client isə eyni qaydanı
/// düyməni aktiv/passiv göstərmək üçün oxuya bilər. Nəticəyə YALNIZ server
/// etibar edir (PRD 156) — client-in "aldım" deməsi kifayət deyil.
/// </para>
/// </summary>
public static class BuyRules
{
    /// <summary>Əşyanın qiyməti; tanınmayan əşya üçün 0.</summary>
    public static int PriceOf(BuyItem item) => item switch
    {
        BuyItem.DefuseKit => GameConstants.DefuseKitPrice,
        _ => 0,
    };

    public static bool IsKnown(BuyItem item) => Enum.IsDefined(item);

    /// <summary>
    /// PRD 8, 27 - Defuse kit yalnız müdafiə tərəfinə satılır: hücum edən
    /// komanda bombanı zərərsizləşdirmir.
    /// </summary>
    public static bool IsAvailableTo(BuyItem item, Team team) => item switch
    {
        BuyItem.DefuseKit => team == Team.Bravo,
        _ => false,
    };

    /// <summary>
    /// Alışın mümkünlüyünü yoxlayır. Uğurlu olarsa <paramref name="remainingMoney"/>
    /// qalıq pulu daşıyır, əks halda cari pul dəyişmir.
    /// </summary>
    public static BuyResultCode Evaluate(
        BuyItem item,
        Team team,
        bool isAlive,
        bool buyEnabled,
        bool alreadyOwned,
        int money,
        out int remainingMoney)
    {
        remainingMoney = money;

        if (!IsKnown(item))
        {
            return BuyResultCode.UnknownItem;
        }

        if (!buyEnabled)
        {
            return BuyResultCode.NotInBuyPhase;
        }

        if (!isAlive)
        {
            return BuyResultCode.PlayerDead;
        }

        if (!IsAvailableTo(item, team))
        {
            return BuyResultCode.WrongTeam;
        }

        if (alreadyOwned)
        {
            return BuyResultCode.AlreadyOwned;
        }

        return EconomyRules.TryPurchase(money, PriceOf(item), out remainingMoney)
            ? BuyResultCode.Purchased
            : BuyResultCode.NotEnoughMoney;
    }
}
