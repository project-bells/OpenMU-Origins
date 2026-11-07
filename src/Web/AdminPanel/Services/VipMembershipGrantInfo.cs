// <copyright file="VipMembershipGrantInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Describes an authoritative VIP membership extension.
/// </summary>
public sealed record VipMembershipGrantInfo(
    Guid Id,
    int DurationDays,
    string Reference,
    DateTime VipUntil,
    DateTime CreatedAt);
