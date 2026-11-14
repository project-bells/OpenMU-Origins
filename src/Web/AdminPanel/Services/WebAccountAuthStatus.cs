// <copyright file="WebAccountAuthStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Result status for web-facing account authentication or password updates.
/// </summary>
public enum WebAccountAuthStatus
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The supplied credentials are invalid.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// The account is banned or temporarily banned.
    /// </summary>
    Banned,

    /// <summary>
    /// The account does not exist.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// The account is currently online and must disconnect before changing sensitive settings.
    /// </summary>
    AccountOnline,

    /// <summary>
    /// The supplied input violates game password or login constraints.
    /// </summary>
    ValidationError,
}

