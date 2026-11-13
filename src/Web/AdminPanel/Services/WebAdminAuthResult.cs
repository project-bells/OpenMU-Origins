// <copyright file="WebAdminAuthResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Verification result for an AdminPanel administrator authentication request.
/// </summary>
/// <param name="Status">The authentication outcome.</param>
/// <param name="LoginName">The canonical admin login name when authenticated.</param>
/// <param name="Roles">Effective assigned roles.</param>
public sealed record WebAdminAuthResult(
    WebAdminAuthStatus Status,
    string? LoginName,
    IReadOnlyList<string> Roles);

