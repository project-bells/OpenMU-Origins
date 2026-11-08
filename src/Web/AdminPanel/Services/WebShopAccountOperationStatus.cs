// <copyright file="WebShopAccountOperationStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The result of queueing a webshop account operation.
/// </summary>
public enum WebShopAccountOperationStatus
{
    /// <summary>
    /// The operation was created.
    /// </summary>
    Created,

    /// <summary>
    /// The exact operation was already queued.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// The account does not exist.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// The reference belongs to another account or payload.
    /// </summary>
    ReferenceConflict,
}
