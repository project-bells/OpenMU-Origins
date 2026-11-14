// <copyright file="CashShopCoinSummary.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The cash shop coins of an account.
/// </summary>
/// <remarks>
/// The balances are the ones which were saved last. When the player is online, the game server
/// may have changed them since then, e.g. by a purchase.
/// </remarks>
/// <param name="LoginName">The login name of the account.</param>
/// <param name="WCoinC">The saved balance of WCoin (C).</param>
/// <param name="WCoinP">The saved balance of WCoin (P).</param>
/// <param name="GoblinPoints">The saved balance of Goblin Points.</param>
/// <param name="Grants">The latest grants, including the pending ones, from the newest to the oldest.</param>
/// <param name="PendingWCoinC">The total unapplied pending WCoin (C) across all grants.</param>
/// <param name="PendingWCoinP">The total unapplied pending WCoin (P) across all grants.</param>
/// <param name="PendingGoblinPoints">The total unapplied pending Goblin Points across all grants.</param>
public record CashShopCoinSummary(
    string LoginName,
    int WCoinC,
    int WCoinP,
    int GoblinPoints,
    IReadOnlyList<CashShopCoinGrantInfo> Grants,
    int PendingWCoinC = 0,
    int PendingWCoinP = 0,
    int PendingGoblinPoints = 0);
