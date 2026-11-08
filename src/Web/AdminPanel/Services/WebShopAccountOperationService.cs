// <copyright file="WebShopAccountOperationService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Queues and queries server-authoritative account operations for an external webshop.
/// </summary>
public sealed class WebShopAccountOperationService
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebShopAccountOperationService"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration source.</param>
    public WebShopAccountOperationService(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
    }

    /// <summary>
    /// Queues an operation, or returns its existing record when the same request is retried.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="reference">The immutable order reference.</param>
    /// <param name="characterName">The first character name.</param>
    /// <param name="newCharacterName">The new character name.</param>
    /// <param name="characterClassNumber">The requested character class.</param>
    /// <param name="amount">The requested amount.</param>
    /// <param name="requestedBy">The authenticated API client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status and operation information.</returns>
    public async ValueTask<(WebShopAccountOperationStatus Status, WebShopAccountOperationInfo? Operation)> QueueAsync(
        string loginName,
        WebShopAccountOperationType operationType,
        string reference,
        string? characterName,
        string? newCharacterName,
        byte? characterClassNumber,
        int? amount,
        string? requestedBy,
        CancellationToken cancellationToken = default)
    {
        reference = reference.Trim();
        using var context = await this.CreateContextAsync(cancellationToken).ConfigureAwait(false);
        if (await context.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return (WebShopAccountOperationStatus.AccountNotFound, null);
        }

        var accountId = account.GetId();
        if (await this.GetExistingAsync(
                reference,
                accountId,
                operationType,
                characterName,
                newCharacterName,
                characterClassNumber,
                amount,
                cancellationToken).ConfigureAwait(false) is { } existing)
        {
            return existing;
        }

        var operation = context.CreateNew<WebShopAccountOperation>();
        operation.AccountId = accountId;
        operation.OperationType = operationType;
        operation.Reference = reference;
        operation.CharacterName = characterName;
        operation.NewCharacterName = newCharacterName;
        operation.CharacterClassNumber = characterClassNumber;
        operation.Amount = amount;
        operation.RequestedBy = requestedBy;
        operation.CreatedAt = DateTime.UtcNow;
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (await this.GetExistingAsync(
                    reference,
                    accountId,
                    operationType,
                    characterName,
                    newCharacterName,
                    characterClassNumber,
                    amount,
                    cancellationToken).ConfigureAwait(false) is { } concurrent)
            {
                return concurrent;
            }

            throw;
        }

        return (WebShopAccountOperationStatus.Created, WebShopAccountOperationInfo.From(operation));
    }

    /// <summary>
    /// Gets an operation by reference when it belongs to the specified account.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="reference">The operation reference.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation, or <see langword="null"/>.</returns>
    public async ValueTask<WebShopAccountOperationInfo?> GetAsync(
        string loginName,
        string reference,
        CancellationToken cancellationToken = default)
    {
        using var context = await this.CreateContextAsync(cancellationToken).ConfigureAwait(false);
        if (await context.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false) is not { } account
            || await context.GetWebShopAccountOperationByReferenceAsync(reference, cancellationToken).ConfigureAwait(false) is not { } operation
            || operation.AccountId != account.GetId())
        {
            return null;
        }

        return WebShopAccountOperationInfo.From(operation);
    }

    private async ValueTask<(WebShopAccountOperationStatus Status, WebShopAccountOperationInfo? Operation)?> GetExistingAsync(
        string reference,
        Guid accountId,
        WebShopAccountOperationType operationType,
        string? characterName,
        string? newCharacterName,
        byte? characterClassNumber,
        int? amount,
        CancellationToken cancellationToken)
    {
        using var context = await this.CreateContextAsync(cancellationToken).ConfigureAwait(false);
        if (await context.GetWebShopAccountOperationByReferenceAsync(reference, cancellationToken).ConfigureAwait(false) is not { } operation)
        {
            return null;
        }

        var samePayload = operation.AccountId == accountId
                          && operation.OperationType == operationType
                          && operation.CharacterName == characterName
                          && operation.NewCharacterName == newCharacterName
                          && operation.CharacterClassNumber == characterClassNumber
                          && operation.Amount == amount;
        return samePayload
            ? (WebShopAccountOperationStatus.AlreadyExists, WebShopAccountOperationInfo.From(operation))
            : (WebShopAccountOperationStatus.ReferenceConflict, null);
    }

    private async ValueTask<IPlayerContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        var configuration = await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        return this._persistenceContextProvider.CreateNewPlayerContext(configuration);
    }
}
