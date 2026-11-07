// <copyright file="CashShopItemGrantInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Describes an authoritative item grant returned to an external shop.
/// </summary>
public sealed record CashShopItemGrantInfo(
    Guid Id,
    int PackageSequence,
    int? PriceSequence,
    int ItemCount,
    string Reference,
    string? GrantedBy,
    string? Reason,
    DateTime CreatedAt)
{
    /// <summary>
    /// Creates the API representation of a persisted grant.
    /// </summary>
    /// <param name="grant">The persisted grant.</param>
    /// <returns>The API representation.</returns>
    public static CashShopItemGrantInfo From(CashShopItemGrant grant)
        => new(
            grant.Id,
            grant.PackageSequence,
            grant.PriceSequence,
            grant.ItemCount,
            grant.Reference,
            grant.GrantedBy,
            grant.Reason,
            grant.CreatedAt);
}
