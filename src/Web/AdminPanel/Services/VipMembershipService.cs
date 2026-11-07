// <copyright file="VipMembershipService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Extends account VIP membership exactly once for a trusted external shop.
/// </summary>
public sealed class VipMembershipService
{
    private const int VipGrantPackageSequence = -1;
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="VipMembershipService"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The provider for player persistence contexts.</param>
    /// <param name="gameConfigurationSource">The game configuration source.</param>
    public VipMembershipService(IPersistenceContextProvider persistenceContextProvider, IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
    }

    /// <summary>
    /// Extends VIP membership by the requested number of days.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="durationDays">The duration to add in days.</param>
    /// <param name="reference">The immutable external order reference.</param>
    /// <param name="reason">The optional audit reason.</param>
    /// <param name="grantedBy">The authenticated service identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grant status and grant information when available.</returns>
    public async ValueTask<(VipMembershipGrantStatus Status, VipMembershipGrantInfo? Grant)> GrantAsync(
        string loginName,
        int durationDays,
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
            return (VipMembershipGrantStatus.AccountNotFound, null);
        }

        if (await context.GetCashShopItemGrantByReferenceAsync(reference, cancellationToken).ConfigureAwait(false) is { } existing)
        {
            return existing.AccountId == account.GetId()
                   && existing.PackageSequence == VipGrantPackageSequence
                   && existing.PriceSequence == durationDays
                ? (VipMembershipGrantStatus.AlreadyGranted, ToInfo(existing, account.VipUntil))
                : (VipMembershipGrantStatus.ReferenceConflict, null);
        }

        var now = DateTime.UtcNow;
        account.VipUntil = (account.VipUntil is { } current && current > now ? current : now).AddDays(durationDays);
        var grant = context.CreateNew<CashShopItemGrant>();
        grant.AccountId = account.GetId();
        grant.PackageSequence = VipGrantPackageSequence;
        grant.PriceSequence = durationDays;
        grant.ItemCount = 0;
        grant.Reference = reference;
        grant.Reason = reason;
        grant.GrantedBy = grantedBy;
        grant.CreatedAt = now;
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            using var retryContext = this._persistenceContextProvider.CreateNewPlayerContext(gameConfiguration);
            var concurrentGrant = await retryContext.GetCashShopItemGrantByReferenceAsync(reference, cancellationToken).ConfigureAwait(false);
            var refreshedAccount = await retryContext.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
            if (concurrentGrant is not null
                && refreshedAccount is not null
                && concurrentGrant.AccountId == refreshedAccount.GetId()
                && concurrentGrant.PackageSequence == VipGrantPackageSequence
                && concurrentGrant.PriceSequence == durationDays)
            {
                return (VipMembershipGrantStatus.AlreadyGranted, ToInfo(concurrentGrant, refreshedAccount.VipUntil));
            }

            throw;
        }

        return (VipMembershipGrantStatus.Created, ToInfo(grant, account.VipUntil));
    }

    private static VipMembershipGrantInfo ToInfo(CashShopItemGrant grant, DateTime? vipUntil)
        => new(grant.Id, grant.PriceSequence ?? 0, grant.Reference, vipUntil ?? grant.CreatedAt, grant.CreatedAt);
}
