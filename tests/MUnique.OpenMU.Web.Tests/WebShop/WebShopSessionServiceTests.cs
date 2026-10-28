// <copyright file="WebShopSessionServiceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.WebShop;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using MUnique.OpenMU.Web.AdminPanel.WebShop;

/// <summary>
/// Tests the protected player session of the web shop.
/// </summary>
[TestFixture]
public class WebShopSessionServiceTests
{
    /// <summary>
    /// Verifies that a protected session can be read on the following request.
    /// </summary>
    [Test]
    public void SignIn_StoresProtectedLoginName()
    {
        var service = new WebShopSessionService(new EphemeralDataProtectionProvider());
        var signInContext = new DefaultHttpContext();

        service.SignIn(signInContext, "buyer");

        var setCookie = signInContext.Response.Headers.SetCookie.ToString();
        var cookie = setCookie.Split(';')[0];
        var requestContext = new DefaultHttpContext();
        requestContext.Request.Headers.Cookie = cookie;
        Assert.That(service.GetLoginName(requestContext), Is.EqualTo("buyer"));
        Assert.That(setCookie, Does.Contain("httponly").IgnoreCase);
    }

    /// <summary>
    /// Verifies that an attacker can't forge the login name cookie.
    /// </summary>
    [Test]
    public void GetLoginName_WithForgedCookie_ReturnsNull()
    {
        var service = new WebShopSessionService(new EphemeralDataProtectionProvider());
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = ".OpenMU.WebShop=buyer";

        Assert.That(service.GetLoginName(context), Is.Null);
    }
}
