// <copyright file="WebShopAccountOperationController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Queues account operations which the game server applies while it exclusively owns the account session.
/// </summary>
[ApiController]
[Route("api/accounts/{loginName}/webshop-operations")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.CashShop)]
public sealed class WebShopAccountOperationController : ControllerBase
{
    private readonly WebShopAccountOperationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebShopAccountOperationController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration source.</param>
    public WebShopAccountOperationController(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._service = new WebShopAccountOperationService(persistenceContextProvider, gameConfigurationSource);
    }

    /// <summary>
    /// Queues an idempotent operation.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="request">The operation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted operation record.</returns>
    [HttpPost]
    public async Task<IActionResult> QueueAsync(
        string loginName,
        [FromBody] WebShopAccountOperationRequest request,
        CancellationToken cancellationToken)
    {
        this.ValidatePayload(request);
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(new ValidationProblemDetails(this.ModelState));
        }

        var (status, operation) = await this._service.QueueAsync(
            loginName,
            request.OperationType!.Value,
            request.Reference!,
            request.CharacterName,
            request.NewCharacterName,
            request.CharacterClassNumber,
            request.Amount,
            this.User.Identity?.Name,
            cancellationToken).ConfigureAwait(false);
        return status switch
        {
            WebShopAccountOperationStatus.Created => this.StatusCode(StatusCodes.Status201Created, operation),
            WebShopAccountOperationStatus.AlreadyExists => this.Ok(operation),
            WebShopAccountOperationStatus.AccountNotFound => this.NotFound(),
            _ => this.Conflict(),
        };
    }

    /// <summary>
    /// Gets the latest status of an operation.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="reference">The immutable operation reference.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation status.</returns>
    [HttpGet("{reference}")]
    public async Task<IActionResult> GetAsync(string loginName, string reference, CancellationToken cancellationToken)
    {
        var operation = await this._service.GetAsync(loginName, reference, cancellationToken).ConfigureAwait(false);
        return operation is null ? this.NotFound() : this.Ok(operation);
    }

    private void ValidatePayload(WebShopAccountOperationRequest request)
    {
        if (request.OperationType is not { } operationType || !Enum.IsDefined(operationType))
        {
            this.ModelState.AddModelError(nameof(request.OperationType), "The operation type is unknown.");
            return;
        }

        var valid = operationType switch
        {
            WebShopAccountOperationType.AddVaultMoney => request.Amount is > 0
                                                        && request.CharacterName is null
                                                        && request.NewCharacterName is null
                                                        && request.CharacterClassNumber is null,
            WebShopAccountOperationType.CreateLevel400Character => request.CharacterName is not null
                                                                    && request.CharacterClassNumber is not null
                                                                    && request.NewCharacterName is null
                                                                    && request.Amount is null,
            WebShopAccountOperationType.RenameCharacter => request.CharacterName is not null
                                                           && request.NewCharacterName is not null
                                                           && request.CharacterClassNumber is null
                                                           && request.Amount is null,
            _ => false,
        };
        if (!valid)
        {
            this.ModelState.AddModelError(string.Empty, "The payload does not match the selected operation type.");
        }
    }
}
