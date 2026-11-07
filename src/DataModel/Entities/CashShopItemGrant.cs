// <copyright file="CashShopItemGrant.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Records an idempotent, server-authoritative grant of configured cash shop products.
/// </summary>
/// <remarks>
/// The referenced products are added to the <see cref="CashShopStorageItem"/> table in the
/// same transaction. This record provides the exactly-once boundary and audit trail for
/// external shops; it must never contain client-supplied item bytes.
/// </remarks>
[AggregateRoot]
public class CashShopItemGrant
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the account which received the products.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the package sequence from the authoritative cash shop configuration.
    /// </summary>
    public int PackageSequence { get; set; }

    /// <summary>
    /// Gets or sets the selected price sequence, or <see langword="null"/> for a bundle.
    /// </summary>
    public int? PriceSequence { get; set; }

    /// <summary>
    /// Gets or sets the number of storage items which were created by the grant.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the unique, immutable reference of the external order.
    /// </summary>
    public string Reference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the authenticated API client which created the grant.
    /// </summary>
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason which is shown in the audit trail.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the time at which the products were granted.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
