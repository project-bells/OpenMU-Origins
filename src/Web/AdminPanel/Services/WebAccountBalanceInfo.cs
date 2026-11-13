// <copyright file="WebAccountBalanceInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Persisted and pending cash-shop coin balances of an account.
/// </summary>
/// <param name="WCoinC">The saved WCoin (C) balance.</param>
/// <param name="WCoinP">The saved WCoin (P) balance.</param>
/// <param name="GoblinPoints">The saved Goblin Points balance.</param>
/// <param name="PendingWCoinC">Total unapplied WCoin (C) grants queued for delivery.</param>
/// <param name="PendingWCoinP">Total unapplied WCoin (P) grants queued for delivery.</param>
/// <param name="PendingGoblinPoints">Total unapplied Goblin Points grants queued for delivery.</param>
public sealed record WebAccountBalanceInfo(
    int WCoinC,
    int WCoinP,
    int GoblinPoints,
    int PendingWCoinC,
    int PendingWCoinP,
    int PendingGoblinPoints);

