// <copyright file="WebShopController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.WebShop;

/// <summary>
/// Handles player login and PayPal checkout redirects for the web shop.
/// </summary>
[AllowAnonymous]
[Route("webshop")]
public sealed class WebShopController : Controller
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;
    private readonly WebShopSessionService _sessionService;
    private readonly PayPalCheckoutService _payPalCheckout;
    private readonly IOptions<WebShopOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebShopController"/> class.
    /// </summary>
    public WebShopController(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource,
        WebShopSessionService sessionService,
        PayPalCheckoutService payPalCheckout,
        IOptions<WebShopOptions> options)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
        this._sessionService = sessionService;
        this._payPalCheckout = payPalCheckout;
        this._options = options;
    }

    /// <summary>
    /// Authenticates a game account for the web shop.
    /// </summary>
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync([FromForm] string loginName, [FromForm] string password, CancellationToken cancellationToken)
    {
        var gameConfiguration = await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(gameConfiguration);
        if (await context.AuthenticateAsync(loginName, password, cancellationToken).ConfigureAwait(false) is null)
        {
            return this.Redirect("/webshop?login=failed");
        }

        this._sessionService.SignIn(this.HttpContext, loginName);
        return this.Redirect("/webshop");
    }

    /// <summary>
    /// Ends the web shop session.
    /// </summary>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        this._sessionService.SignOut(this.HttpContext);
        return this.Redirect("/webshop");
    }

    /// <summary>
    /// Creates a PayPal order for a configured package.
    /// </summary>
    [HttpPost("checkout/{packageCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutAsync(string packageCode, CancellationToken cancellationToken)
    {
        if (this._sessionService.GetLoginName(this.HttpContext) is not { } loginName)
        {
            return this.Redirect("/webshop");
        }

        var options = this._options.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.PayPalClientId) || string.IsNullOrWhiteSpace(options.PayPalClientSecret))
        {
            return this.Redirect("/webshop?payment=unavailable");
        }

        var package = options.Packages.SingleOrDefault(candidate => string.Equals(candidate.Code, packageCode, StringComparison.Ordinal));
        if (package is null)
        {
            return this.NotFound();
        }

        var returnUrl = new Uri(this.Url.ActionLink(nameof(this.Completed), values: new { packageCode })!);
        var cancelUrl = new Uri(this.Url.ActionLink(nameof(this.Cancelled))!);
        var approvalUrl = await this._payPalCheckout.CreateOrderAsync(package, loginName, returnUrl, cancelUrl, cancellationToken).ConfigureAwait(false);
        return this.Redirect(approvalUrl.AbsoluteUri);
    }

    /// <summary>
    /// Displays the phase-one completion result. Capture and coin granting are added in phase two.
    /// </summary>
    [HttpGet("completed")]
    public IActionResult Completed(string? packageCode, string? token)
    {
        if (string.IsNullOrWhiteSpace(packageCode) || string.IsNullOrWhiteSpace(token))
        {
            return this.Redirect("/webshop?payment=invalid");
        }

        return this.Redirect($"/webshop?payment=approved&package={Uri.EscapeDataString(packageCode)}&order={Uri.EscapeDataString(token)}");
    }

    /// <summary>
    /// Displays a cancelled checkout.
    /// </summary>
    [HttpGet("cancelled")]
    public IActionResult Cancelled() => this.Redirect("/webshop?payment=cancelled");
}
