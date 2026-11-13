// <copyright file="AdminAuthenticateRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// AdminPanel operator credential verification request used by the website GM console BFF.
/// </summary>
public sealed class AdminAuthenticateRequest
{
    /// <summary>
    /// Gets or sets the admin login name.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional authenticator TOTP or recovery code.
    /// </summary>
    [StringLength(64)]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="TwoFactorCode"/> is a recovery code.
    /// </summary>
    public bool IsRecoveryCode { get; set; }
}

