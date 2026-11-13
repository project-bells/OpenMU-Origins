// <copyright file="AccountSelfPasswordChange.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Player self-service password change request requiring verification of the current password.
/// </summary>
public sealed class AccountSelfPasswordChange
{
    /// <summary>
    /// Gets or sets the current password to verify before updating.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password matching game limits.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
}

