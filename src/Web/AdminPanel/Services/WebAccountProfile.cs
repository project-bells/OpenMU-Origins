// <copyright file="WebAccountProfile.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Safe account profile payload returned to the website backend (never contains credential hashes or security codes).
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="LoginName">The account login name.</param>
/// <param name="EMail">The account e-mail address.</param>
/// <param name="State">The current account state.</param>
/// <param name="RegistrationDate">The UTC registration timestamp.</param>
/// <param name="VipUntil">The UTC timestamp until VIP membership remains active, if any.</param>
/// <param name="OnlineTimeSeconds">Authoritative cumulative online seconds recorded by the game server.</param>
/// <param name="Online">Whether the account currently has an active login session.</param>
/// <param name="ServerId">The game server identifier where the account is online, if any.</param>
/// <param name="Balance">Saved and pending cash shop coin balances.</param>
/// <param name="Characters">Characters belonging to this account.</param>
public sealed record WebAccountProfile(
    Guid Id,
    string LoginName,
    string EMail,
    string State,
    DateTime RegistrationDate,
    DateTime? VipUntil,
    long OnlineTimeSeconds,
    bool Online,
    int? ServerId,
    WebAccountBalanceInfo Balance,
    IReadOnlyList<WebCharacterInfo> Characters);

