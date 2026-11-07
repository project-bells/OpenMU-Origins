// <copyright file="VipMembershipGrantRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Requests an exactly-once extension of an account's VIP membership.
/// </summary>
public sealed class VipMembershipGrantRequest
{
    /// <summary>
    /// Gets or sets the duration in days. Supported memberships are one to twelve 30-day periods.
    /// </summary>
    [Range(30, 360)]
    public int DurationDays { get; set; }

    /// <summary>
    /// Gets or sets the immutable external order reference.
    /// </summary>
    [Required]
    [StringLength(CashShopItemGrantRequest.ReferenceMaximumLength, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9:._/-]*$")]
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets the audit reason.
    /// </summary>
    [StringLength(CashShopItemGrantRequest.ReasonMaximumLength)]
    public string? Reason { get; set; }
}
