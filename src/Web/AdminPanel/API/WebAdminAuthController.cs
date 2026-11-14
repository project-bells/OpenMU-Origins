// <copyright file="WebAdminAuthController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Internal operator verification endpoint for authenticating GM sessions from the website BFF.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public sealed class WebAdminAuthController : ControllerBase
{
    private readonly WebAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAdminAuthController"/> class.
    /// </summary>
    /// <param name="accountService">The web account service.</param>
    public WebAdminAuthController(WebAccountService accountService)
    {
        this._accountService = accountService;
    }

    /// <summary>
    /// Verifies an AdminPanel operator account's password and returns effective roles without issuing AdminPanel cookies.
    /// </summary>
    /// <param name="request">The admin login credentials.</param>
    /// <returns>Verification status and assigned roles when valid.</returns>
    [HttpPost("verify")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme, Policy = AdminPolicies.AuthenticatedOperator)]
    public async Task<IActionResult> VerifyAsync([FromBody] AdminAuthenticateRequest request)
    {
        var result = await this._accountService.AuthenticateAdminAsync(
            request.LoginName,
            request.Password,
            request.TwoFactorCode,
            request.IsRecoveryCode).ConfigureAwait(false);

        return result.Status switch
        {
            WebAdminAuthStatus.Success => this.Ok(new
            {
                authenticated = true,
                loginName = result.LoginName,
                roles = result.Roles,
            }),
            WebAdminAuthStatus.TwoFactorRequired => this.StatusCode(StatusCodes.Status401Unauthorized, new
            {
                authenticated = false,
                code = "TWO_FACTOR_REQUIRED",
                message = "Two-factor authenticator code is required.",
            }),
            WebAdminAuthStatus.LockedOut => this.StatusCode(StatusCodes.Status423Locked, new
            {
                authenticated = false,
                code = "LOCKED_OUT",
                message = "Admin account is temporarily locked due to repeated failed attempts.",
            }),
            WebAdminAuthStatus.Unavailable => this.StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                authenticated = false,
                code = "ADMIN_STORE_UNAVAILABLE",
                message = "Admin authentication store is not configured.",
            }),
            _ => this.Unauthorized(new
            {
                authenticated = false,
                code = "INVALID_CREDENTIALS",
                message = "Invalid administrator credentials.",
            }),
        };
    }
}

