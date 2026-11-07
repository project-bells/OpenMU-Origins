// <copyright file="CashShopItemGrantService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Grants configured cash shop products without accepting item data from an external shop.
/// </summary>
public sealed class CashShopItemGrantService
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopItemGrantService"/> class.
    /// </summary>
    public CashShopItemGrantService(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
    }

    /// <summary>
    /// Adds the configured products to the account's cash shop storage exactly once.
    /// </summary>
    public async ValueTask<(CashShopItemGrantStatus Status, CashShopItemGrantInfo? Grant)> GrantAsync(
        string loginName,
        int packageSequence,
        int? priceSequence,
        string reference,
        string? reason,
        string? grantedBy,
        CancellationToken cancellationToken = default)
    {
        reference = reference.Trim();
        var gameConfiguration = await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(gameConfiguration);
        if (await context.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return (CashShopItemGrantStatus.AccountNotFound, null);
        }

        var accountId = account.GetId();
        if (await this.GetExistingGrantAsync(reference, accountId, packageSequence, priceSequence, gameConfiguration, cancellationToken).ConfigureAwait(false) is { } existing)
        {
            return existing;
        }

        if (!TryGetProducts(gameConfiguration, packageSequence, priceSequence, out var products))
        {
            return (CashShopItemGrantStatus.ProductNotFound, null);
        }

        var createdAt = DateTime.UtcNow;
        foreach (var product in products)
        {
            var storageItem = context.CreateNew<CashShopStorageItem>();
            storageItem.AccountId = accountId;
            storageItem.ProductSequence = product.ProductSequence;
            storageItem.PriceSequence = product.PriceSequence;
            storageItem.AddedAt = createdAt;
        }

        var grant = context.CreateNew<CashShopItemGrant>();
        grant.AccountId = accountId;
        grant.PackageSequence = packageSequence;
        grant.PriceSequence = priceSequence;
        grant.ItemCount = products.Count;
        grant.Reference = reference;
        grant.Reason = reason;
        grant.GrantedBy = grantedBy;
        grant.CreatedAt = createdAt;
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (await this.GetExistingGrantAsync(reference, accountId, packageSequence, priceSequence, gameConfiguration, cancellationToken).ConfigureAwait(false) is { } concurrent)
            {
                return concurrent;
            }

            throw;
        }

        return (CashShopItemGrantStatus.Created, CashShopItemGrantInfo.From(grant));
    }

    private static bool TryGetProducts(
        GameConfiguration gameConfiguration,
        int packageSequence,
        int? priceSequence,
        out IReadOnlyCollection<CashShopProduct> products)
    {
        var package = gameConfiguration.CashShopConfiguration?.Packages
            .SingleOrDefault(candidate => candidate.PackageSequence == packageSequence && candidate.IsForSale);
        if (package is null)
        {
            products = [];
            return false;
        }

        if (package.IsBundle)
        {
            products = priceSequence is null ? package.Products.ToList() : [];
        }
        else
        {
            var product = priceSequence is null
                ? null
                : package.Products.SingleOrDefault(candidate => candidate.PriceSequence == priceSequence.Value);
            products = product is null ? [] : [product];
        }

        return products.Count > 0;
    }

    private async ValueTask<(CashShopItemGrantStatus Status, CashShopItemGrantInfo? Grant)?> GetExistingGrantAsync(
        string reference,
        Guid accountId,
        int packageSequence,
        int? priceSequence,
        GameConfiguration gameConfiguration,
        CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(gameConfiguration);
        if (await context.GetCashShopItemGrantByReferenceAsync(reference, cancellationToken).ConfigureAwait(false) is not { } grant)
        {
            return null;
        }

        return grant.AccountId == accountId
            && grant.PackageSequence == packageSequence
            && grant.PriceSequence == priceSequence
                ? (CashShopItemGrantStatus.AlreadyGranted, CashShopItemGrantInfo.From(grant))
                : (CashShopItemGrantStatus.ReferenceConflict, null);
    }
}
