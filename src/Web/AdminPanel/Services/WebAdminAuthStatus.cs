// <copyright file="WebAdminAuthStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Result status when verifying an AdminPanel operator account for the external website console.
/// </summary>
public enum WebAdminAuthStatus
{
    /// <summary>
    /// Verification succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// Credentials are invalid or account is disabled.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// Account is locked out due to repeated failed attempts.
    /// </summary>
    LockedOut,

    /// <summary>
    /// Account requires a two-factor authenticator or recovery code.
    /// </summary>
    TwoFactorRequired,

    /// <summary>
    /// Admin user store is not configured or unavailable.
    /// </summary>
    Unavailable,
}

