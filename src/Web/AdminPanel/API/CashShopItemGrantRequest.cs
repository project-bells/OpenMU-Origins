// <copyright file="CashShopItemGrantRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Requests server-authoritative cash shop products for an account.
/// </summary>
public sealed class CashShopItemGrantRequest
{
    /// <summary>
    /// The maximum length of an external order reference.
    /// </summary>
    public const int ReferenceMaximumLength = 128;

    /// <summary>
    /// The maximum length of an audit reason.
    /// </summary>
    public const int ReasonMaximumLength = 256;

    /// <summary>
    /// Gets or sets the configured package sequence.
    /// </summary>
    [Required]
    public int? PackageSequence { get; set; }

    /// <summary>
    /// Gets or sets the selected price sequence. It must be omitted for a bundle package.
    /// </summary>
    public int? PriceSequence { get; set; }

    /// <summary>
    /// Gets or sets the immutable external order reference.
    /// </summary>
    [Required]
    [StringLength(ReferenceMaximumLength, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9:._/-]*$")]
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets the audit reason.
    /// </summary>
    [StringLength(ReasonMaximumLength)]
    public string? Reason { get; set; }
}
