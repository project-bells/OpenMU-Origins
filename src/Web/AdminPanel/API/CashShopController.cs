// <copyright file="CashShopController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Operations;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The public API to show and grant the cash shop coins of accounts, e.g. for a payment provider.
/// </summary>
[ApiController]
[Route("api/accounts/{loginName}/cash-shop")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.AuthenticatedCashShop)]
public class CashShopController : ControllerBase
{
    private readonly CashShopCoinService _coinService;
    private readonly CashShopItemGrantService _itemGrantService;
    private readonly VipMembershipService _vipMembershipService;
    private readonly AdminOperationExecutor _executor;
    private readonly ILogger<CashShopController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration source.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="executor">The shared audit executor.</param>
    public CashShopController(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource,
        ILogger<CashShopController> logger,
        AdminOperationExecutor executor)
    {
        this._coinService = new CashShopCoinService(persistenceContextProvider, gameConfigurationSource);
        this._itemGrantService = new CashShopItemGrantService(persistenceContextProvider, gameConfigurationSource);
        this._vipMembershipService = new VipMembershipService(persistenceContextProvider, gameConfigurationSource);
        this._logger = logger;
        this._executor = executor;
    }

    /// <summary>
    /// Gets the cash shop coins of an account, with its latest grants.
    /// </summary>
    /// <param name="loginName">The login name of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The coins of the account; 404, if the account doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAsync(string loginName, CancellationToken cancellationToken)
    {
        var summary = await this._coinService.GetSummaryAsync(loginName, cancellationToken).ConfigureAwait(false);
        return summary is null ? this.NotFound() : this.Ok(summary);
    }

    /// <summary>
    /// Grants cash shop coins to an account. The game server applies them the next time the player opens the cash shop.
    /// </summary>
    /// <param name="loginName">The login name of the account.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// 201 with the created grant;
    /// 200 with the existing grant, if the account already got a grant with the same reference;
    /// 400, if the request is invalid;
    /// 404, if the account doesn't exist;
    /// 409, if another account already got a grant with the same reference.
    /// </returns>
    [HttpPost("grants")]
    public async Task<IActionResult> GrantAsync(string loginName, [FromBody] CashShopCoinGrantRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount is null or 0)
        {
            this.ModelState.AddModelError(nameof(request.Amount), "The amount must not be zero.");
        }

        if (request.CoinType is not { } requestedCoinType || !Enum.IsDefined(requestedCoinType))
        {
            this.ModelState.AddModelError(nameof(request.CoinType), "The coin type is unknown.");
        }

        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(new ValidationProblemDetails(this.ModelState));
        }

        var amount = request.Amount!.Value;
        var coinType = request.CoinType!.Value;

        var clientName = this.User.Identity?.Name;
        var result = await this._executor.ExecuteAsync(
            AdminRoles.CashShop,
            "cashshop_grant",
            new { loginName, coinType = coinType.ToString(), amount, request.Reason, request.Reference },
            async actor =>
            {
                var (grantStatus, createdGrant) = await this._coinService.GrantAsync(loginName, coinType, amount, request.Reason, request.Reference, actor.Name, cancellationToken).ConfigureAwait(false);
                return new { Status = grantStatus, Grant = createdGrant };
            },
            cancellationToken,
            value => value.Status is CashShopCoinGrantStatus.Created or CashShopCoinGrantStatus.AlreadyGranted).ConfigureAwait(false);
        var status = result.Status;
        var grant = result.Grant;
        switch (status)
        {
            case CashShopCoinGrantStatus.Created:
                this._logger.LogInformation("API client {Client} granted {Amount} {CoinType} to account {LoginName}, reference {Reference}.", clientName, amount, coinType, loginName, request.Reference);
                return this.StatusCode(StatusCodes.Status201Created, grant);
            case CashShopCoinGrantStatus.AlreadyGranted:
                return this.Ok(grant);
            case CashShopCoinGrantStatus.AccountNotFound:
                return this.NotFound();
            default:
                return this.Conflict();
        }
    }

    /// <summary>
    /// Grants products from the authoritative server cash shop configuration exactly once.
    /// </summary>
    /// <remarks>
    /// The caller only selects configured sequence numbers. Item bytes, options, serials and
    /// prices are deliberately not accepted from the caller.
    /// </remarks>
    [HttpPost("item-grants")]
    public async Task<IActionResult> GrantItemAsync(
        string loginName,
        [FromBody] CashShopItemGrantRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            this.ModelState.AddModelError(nameof(request.Reference), "The reference must not be empty.");
        }

        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(new ValidationProblemDetails(this.ModelState));
        }

        var clientName = this.User.Identity?.Name;
        var (status, grant) = await this._itemGrantService.GrantAsync(
            loginName,
            request.PackageSequence!.Value,
            request.PriceSequence,
            request.Reference!,
            request.Reason,
            clientName,
            cancellationToken).ConfigureAwait(false);
        switch (status)
        {
            case CashShopItemGrantStatus.Created:
                this._logger.LogInformation(
                    "API client {Client} granted cash shop package {PackageSequence}, price {PriceSequence} to account {LoginName}, reference {Reference}.",
                    clientName,
                    request.PackageSequence,
                    request.PriceSequence,
                    loginName,
                    request.Reference);
                return this.StatusCode(StatusCodes.Status201Created, grant);
            case CashShopItemGrantStatus.AlreadyGranted:
                return this.Ok(grant);
            case CashShopItemGrantStatus.AccountNotFound:
                return this.NotFound();
            case CashShopItemGrantStatus.ProductNotFound:
                return this.UnprocessableEntity();
            default:
                return this.Conflict();
        }
    }

    /// <summary>
    /// Extends the account's VIP membership exactly once.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="request">The membership grant request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative membership grant result.</returns>
    [HttpPost("vip-grants")]
    public async Task<IActionResult> GrantVipAsync(
        string loginName,
        [FromBody] VipMembershipGrantRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DurationDays % 30 != 0)
        {
            this.ModelState.AddModelError(nameof(request.DurationDays), "The duration must be a whole number of 30-day periods.");
        }

        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(new ValidationProblemDetails(this.ModelState));
        }

        var clientName = this.User.Identity?.Name;
        var (status, grant) = await this._vipMembershipService.GrantAsync(
            loginName,
            request.DurationDays,
            request.Reference!,
            request.Reason,
            clientName,
            cancellationToken).ConfigureAwait(false);
        return status switch
        {
            VipMembershipGrantStatus.Created => this.StatusCode(StatusCodes.Status201Created, grant),
            VipMembershipGrantStatus.AlreadyGranted => this.Ok(grant),
            VipMembershipGrantStatus.AccountNotFound => this.NotFound(),
            _ => this.Conflict(),
        };
    }
}
