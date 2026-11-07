// <copyright file="CashShopItemGrantStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The result of an authoritative item grant.
/// </summary>
public enum CashShopItemGrantStatus
{
    /// <summary>
    /// The grant and its storage items were created.
    /// </summary>
    Created,

    /// <summary>
    /// The same reference and request were granted before.
    /// </summary>
    AlreadyGranted,

    /// <summary>
    /// The account does not exist.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// The requested package or product is not offered by the server.
    /// </summary>
    ProductNotFound,

    /// <summary>
    /// The reference belongs to another account or a different product selection.
    /// </summary>
    ReferenceConflict,
}
