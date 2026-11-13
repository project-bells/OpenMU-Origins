// <copyright file="AccountAuthenticateRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Player account credential verification request submitted by the website BFF.
/// </summary>
public sealed class AccountAuthenticateRequest
{
    /// <summary>
    /// Gets or sets the login name.
    /// </summary>
    [Required]
    [MaxLength(AccountRegistration.LoginNameMaximumLength)]
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password. Never logged or persisted outside credential verification.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

