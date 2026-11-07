// <copyright file="VipMembershipGrantStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The result of an authoritative VIP membership grant.
/// </summary>
public enum VipMembershipGrantStatus
{
    /// <summary>The membership was extended.</summary>
    Created,

    /// <summary>The same extension was already applied.</summary>
    AlreadyGranted,

    /// <summary>The account does not exist.</summary>
    AccountNotFound,

    /// <summary>The reference belongs to another grant.</summary>
    ReferenceConflict,
}
