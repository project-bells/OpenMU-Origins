// <copyright file="WebShopSessionService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.WebShop;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Protects the player identity stored in the web shop cookie.
/// </summary>
public sealed class WebShopSessionService
{
    private const string CookieName = ".OpenMU.WebShop";
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebShopSessionService"/> class.
    /// </summary>
    public WebShopSessionService(IDataProtectionProvider dataProtectionProvider)
    {
        this._protector = dataProtectionProvider.CreateProtector("MUnique.OpenMU.WebShop.PlayerSession.v1");
    }

    /// <summary>
    /// Gets the authenticated game account login name, if present.
    /// </summary>
    public string? GetLoginName(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var protectedLoginName))
        {
            return null;
        }

        try
        {
            return this._protector.Unprotect(protectedLoginName);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// Starts a player web shop session.
    /// </summary>
    public void SignIn(HttpContext context, string loginName)
    {
        context.Response.Cookies.Append(CookieName, this._protector.Protect(loginName), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromHours(2),
        });
    }

    /// <summary>
    /// Ends the player web shop session.
    /// </summary>
    public void SignOut(HttpContext context) => context.Response.Cookies.Delete(CookieName);
}
