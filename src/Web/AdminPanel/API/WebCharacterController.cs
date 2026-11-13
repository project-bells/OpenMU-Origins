// <copyright file="WebCharacterController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Server-authoritative offline character self-service endpoints for the integrated website BFF.
/// </summary>
[ApiController]
[Route("api/accounts/{loginName}/characters/{characterName}")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.AuthenticatedOperator)]
public sealed class WebCharacterController : ControllerBase
{
    private readonly WebCharacterSelfService _characterService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebCharacterController"/> class.
    /// </summary>
    /// <param name="characterService">The offline character self-service handler.</param>
    public WebCharacterController(WebCharacterSelfService characterService)
    {
        this._characterService = characterService;
    }

    /// <summary>
    /// Performs an offline character reset subject to server <c>ResetConfiguration</c>.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated character payload or domain error.</returns>
    [HttpPost("reset")]
    public async ValueTask<IActionResult> ResetAsync(string loginName, string characterName, CancellationToken cancellationToken)
    {
        var result = await this._characterService.ResetCharacterAsync(loginName, characterName, cancellationToken).ConfigureAwait(false);
        return this.MapResult(result);
    }

    /// <summary>
    /// Clears PK status and kill count while offline subject to <c>PKClearConfiguration</c>.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated character payload or domain error.</returns>
    [HttpPost("pk-clear")]
    public async ValueTask<IActionResult> ClearPkAsync(string loginName, string characterName, CancellationToken cancellationToken)
    {
        var result = await this._characterService.ClearPkAsync(loginName, characterName, cancellationToken).ConfigureAwait(false);
        return this.MapResult(result);
    }

    /// <summary>
    /// Allocates unspent stat points to base character stats while offline.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="request">Stat point increments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated character payload or domain error.</returns>
    [HttpPost("add-stats")]
    public async ValueTask<IActionResult> AddStatsAsync(
        string loginName,
        string characterName,
        [FromBody] CharacterAddStatsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this._characterService.AddStatsAsync(
            loginName,
            characterName,
            request.Strength,
            request.Agility,
            request.Vitality,
            request.Energy,
            request.Leadership,
            cancellationToken).ConfigureAwait(false);
        return this.MapResult(result);
    }

    /// <summary>
    /// Refunds invested character stats back to class base values while offline.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated character payload or domain error.</returns>
    [HttpPost("reset-stats")]
    public async ValueTask<IActionResult> ResetStatsAsync(string loginName, string characterName, CancellationToken cancellationToken)
    {
        var result = await this._characterService.ResetStatsAsync(loginName, characterName, cancellationToken).ConfigureAwait(false);
        return this.MapResult(result);
    }

    private IActionResult MapResult(WebCharacterOperationResult result)
    {
        if (result.Status == WebCharacterOperationStatus.Success)
        {
            return this.Ok(new
            {
                success = true,
                message = result.Message,
                zenSpent = result.ZenSpent,
                pointsDelta = result.PointsDelta,
                character = result.Character,
            });
        }

        var statusCode = result.Status switch
        {
            WebCharacterOperationStatus.AccountNotFound or WebCharacterOperationStatus.CharacterNotFound => StatusCodes.Status404NotFound,
            WebCharacterOperationStatus.AccountOnline => StatusCodes.Status409Conflict,
            WebCharacterOperationStatus.AccountBanned => StatusCodes.Status403Forbidden,
            WebCharacterOperationStatus.FeatureDisabled => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        return this.StatusCode(statusCode, new
        {
            success = false,
            code = result.Status.ToString(),
            message = result.Message,
        });
    }
}

