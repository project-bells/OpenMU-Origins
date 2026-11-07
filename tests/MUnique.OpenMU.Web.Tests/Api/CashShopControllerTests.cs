// <copyright file="CashShopControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.API;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Tests for the <see cref="CashShopController"/>, which grants cash shop coins through the public API.
/// </summary>
[TestFixture]
public class CashShopControllerTests
{
    private InMemoryPersistenceContextProvider _persistenceContextProvider = null!;

    private CashShopController _controller = null!;

    private GameConfiguration _gameConfiguration = null!;

    private IDataSource<GameConfiguration> _gameConfigurationSource = null!;

    /// <summary>
    /// Sets up an in-memory database with two accounts, and the controller.
    /// </summary>
    /// <returns>The task.</returns>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._persistenceContextProvider = new InMemoryPersistenceContextProvider();
        this._gameConfiguration = CreateGameConfiguration();
        var gameConfigurationSource = new Mock<IDataSource<GameConfiguration>>();
        gameConfigurationSource
            .Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this._gameConfiguration);
        this._gameConfigurationSource = gameConfigurationSource.Object;
        this._controller = new CashShopController(this._persistenceContextProvider, this._gameConfigurationSource, Mock.Of<ILogger<CashShopController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        using var context = this._persistenceContextProvider.CreateNewPlayerContext(new GameConfiguration());
        foreach (var loginName in new[] { "buyer", "other" })
        {
            var account = context.CreateNew<Account>();
            account.LoginName = loginName;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// A grant is created as pending, and shows up in the summary of the account.
    /// </summary>
    [Test]
    public async Task GrantCreatesPendingGrantAsync()
    {
        var result = await this._controller.GrantAsync("buyer", CreateRequest(100, "payment-1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        var grant = (CashShopCoinGrantInfo)((ObjectResult)result).Value!;
        Assert.That(grant.Amount, Is.EqualTo(100));
        Assert.That(grant.CoinType, Is.EqualTo(CashShopCoinType.WCoinC));
        Assert.That(grant.AppliedAt, Is.Null);

        var summary = (CashShopCoinSummary)((OkObjectResult)await this._controller.GetAsync("buyer", CancellationToken.None).ConfigureAwait(false)).Value!;
        Assert.That(summary.WCoinC, Is.Zero, "The game server applies the grant, not the API.");
        Assert.That(summary.Grants.Select(g => g.Id), Is.EqualTo(new[] { grant.Id }));
    }

    /// <summary>
    /// A repeated request with the same reference doesn't grant the coins twice, but answers with the existing grant.
    /// </summary>
    [Test]
    public async Task RepeatedReferenceIsGrantedOnceAsync()
    {
        var first = (CashShopCoinGrantInfo)((ObjectResult)await this._controller.GrantAsync("buyer", CreateRequest(100, "payment-1"), CancellationToken.None).ConfigureAwait(false)).Value!;

        var result = await this._controller.GrantAsync("buyer", CreateRequest(100, "payment-1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((CashShopCoinGrantInfo)((OkObjectResult)result).Value!).Id, Is.EqualTo(first.Id));
        var summary = (CashShopCoinSummary)((OkObjectResult)await this._controller.GetAsync("buyer", CancellationToken.None).ConfigureAwait(false)).Value!;
        Assert.That(summary.Grants, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// A reference which another account already got is a conflict.
    /// </summary>
    [Test]
    public async Task ReferenceOfOtherAccountIsConflictAsync()
    {
        await this._controller.GrantAsync("other", CreateRequest(100, "payment-1"), CancellationToken.None).ConfigureAwait(false);

        var result = await this._controller.GrantAsync("buyer", CreateRequest(100, "payment-1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<ConflictResult>());
    }

    /// <summary>
    /// Grants without reference are always created.
    /// </summary>
    [Test]
    public async Task GrantsWithoutReferenceAreCreatedAsync()
    {
        await this._controller.GrantAsync("buyer", CreateRequest(100, null), CancellationToken.None).ConfigureAwait(false);
        var result = await this._controller.GrantAsync("buyer", CreateRequest(-30, " "), CancellationToken.None).ConfigureAwait(false);

        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        var summary = (CashShopCoinSummary)((OkObjectResult)await this._controller.GetAsync("buyer", CancellationToken.None).ConfigureAwait(false)).Value!;
        Assert.That(summary.Grants.Select(g => g.Amount), Is.EquivalentTo(new[] { 100, -30 }));
    }

    /// <summary>
    /// An unknown account is answered with not found, and a zero amount or an unknown coin type is invalid.
    /// A grant of an unknown coin type couldn't be applied by the game server.
    /// </summary>
    [Test]
    public async Task InvalidRequestsAsync()
    {
        var unknownCoinType = CreateRequest(100, null);
        unknownCoinType.CoinType = (CashShopCoinType)7;

        Assert.That(await this._controller.GrantAsync("unknown", CreateRequest(100, null), CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<NotFoundResult>());
        Assert.That(await this._controller.GetAsync("unknown", CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<NotFoundResult>());
        Assert.That(await this._controller.GrantAsync("buyer", CreateRequest(0, null), CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<ObjectResult>().With.Property(nameof(ObjectResult.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(await this._controller.GrantAsync("buyer", unknownCoinType, CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<ObjectResult>().With.Property(nameof(ObjectResult.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// An item grant creates storage entries from the server configuration, without accepting item data.
    /// </summary>
    [Test]
    public async Task ItemGrantCreatesConfiguredStorageItemAsync()
    {
        var result = await this._controller.GrantItemAsync(
            "buyer",
            CreateItemGrantRequest(10, 101, "order-1"),
            CancellationToken.None).ConfigureAwait(false);

        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        var grant = (CashShopItemGrantInfo)((ObjectResult)result).Value!;
        Assert.That(grant.ItemCount, Is.EqualTo(1));

        using var context = this._persistenceContextProvider.CreateNewPlayerContext(this._gameConfiguration);
        var account = await context.GetAccountByLoginNameAsync("buyer").ConfigureAwait(false);
        var storageItems = await context.GetCashShopStorageItemsAsync(account!.GetId()).ConfigureAwait(false);
        Assert.That(storageItems, Has.Count.EqualTo(1));
        Assert.That(storageItems[0].ProductSequence, Is.EqualTo(1001));
        Assert.That(storageItems[0].PriceSequence, Is.EqualTo(101));
    }

    /// <summary>
    /// Retrying the same external order returns the original grant and never duplicates storage items.
    /// </summary>
    [Test]
    public async Task ItemGrantReferenceIsExactlyOnceAsync()
    {
        var request = CreateItemGrantRequest(10, 101, "order-1");
        var firstResult = (ObjectResult)await this._controller.GrantItemAsync("buyer", request, CancellationToken.None).ConfigureAwait(false);
        var first = (CashShopItemGrantInfo)firstResult.Value!;

        var retry = await this._controller.GrantItemAsync("buyer", request, CancellationToken.None).ConfigureAwait(false);

        Assert.That(retry, Is.InstanceOf<OkObjectResult>());
        Assert.That(((CashShopItemGrantInfo)((OkObjectResult)retry).Value!).Id, Is.EqualTo(first.Id));
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(this._gameConfiguration);
        var account = await context.GetAccountByLoginNameAsync("buyer").ConfigureAwait(false);
        Assert.That(await context.GetCashShopStorageItemsAsync(account!.GetId()).ConfigureAwait(false), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Reusing an order reference for another product or account is rejected.
    /// </summary>
    [Test]
    public async Task ItemGrantReferenceMismatchIsConflictAsync()
    {
        await this._controller.GrantItemAsync("buyer", CreateItemGrantRequest(10, 101, "order-1"), CancellationToken.None).ConfigureAwait(false);

        var changedProduct = await this._controller.GrantItemAsync("buyer", CreateItemGrantRequest(10, 102, "order-1"), CancellationToken.None).ConfigureAwait(false);
        var changedAccount = await this._controller.GrantItemAsync("other", CreateItemGrantRequest(10, 101, "order-1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(changedProduct, Is.InstanceOf<ConflictResult>());
        Assert.That(changedAccount, Is.InstanceOf<ConflictResult>());
    }

    /// <summary>
    /// Unknown, disabled, and ambiguous package selections are rejected without creating storage items.
    /// </summary>
    [Test]
    public async Task InvalidItemSelectionIsRejectedAsync()
    {
        var unknown = await this._controller.GrantItemAsync("buyer", CreateItemGrantRequest(999, null, "unknown"), CancellationToken.None).ConfigureAwait(false);
        var bundleWithPrice = await this._controller.GrantItemAsync("buyer", CreateItemGrantRequest(20, 201, "bundle-price"), CancellationToken.None).ConfigureAwait(false);
        var optionWithoutPrice = await this._controller.GrantItemAsync("buyer", CreateItemGrantRequest(10, null, "missing-price"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(unknown, Is.InstanceOf<UnprocessableEntityResult>());
        Assert.That(bundleWithPrice, Is.InstanceOf<UnprocessableEntityResult>());
        Assert.That(optionWithoutPrice, Is.InstanceOf<UnprocessableEntityResult>());
    }

    /// <summary>
    /// A bundle grant atomically creates every configured product in the storage.
    /// </summary>
    [Test]
    public async Task BundleGrantCreatesAllConfiguredStorageItemsAsync()
    {
        var result = (ObjectResult)await this._controller.GrantItemAsync(
            "buyer",
            CreateItemGrantRequest(20, null, "bundle-1"),
            CancellationToken.None).ConfigureAwait(false);

        Assert.That(((CashShopItemGrantInfo)result.Value!).ItemCount, Is.EqualTo(2));
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(this._gameConfiguration);
        var account = await context.GetAccountByLoginNameAsync("buyer").ConfigureAwait(false);
        var storageItems = await context.GetCashShopStorageItemsAsync(account!.GetId()).ConfigureAwait(false);
        Assert.That(storageItems.Select(item => item.ProductSequence), Is.EquivalentTo(new[] { 2001, 2002 }));
    }

    /// <summary>
    /// The trusted catalog endpoint only exposes enabled packages and their immutable sequence identifiers.
    /// </summary>
    [Test]
    public async Task AuthoritativeCatalogContainsEnabledOffersAsync()
    {
        var controller = new CashShopCatalogController(this._gameConfigurationSource);

        var result = (OkObjectResult)await controller.GetAsync(CancellationToken.None).ConfigureAwait(false);

        var packages = (IReadOnlyList<CashShopPackageInfo>)result.Value!;
        Assert.That(packages.Select(package => package.PackageSequence), Is.EqualTo(new[] { 10, 20 }));
        Assert.That(packages[0].Products.Select(product => (product.ProductSequence, product.PriceSequence)),
            Is.EqualTo(new[] { (1001, 101), (1002, 102) }));
    }

    /// <summary>
    /// A VIP grant extends the authoritative account expiry and a replay never extends it twice.
    /// </summary>
    [Test]
    public async Task VipMembershipGrantIsExactlyOnceAsync()
    {
        var request = new VipMembershipGrantRequest { DurationDays = 30, Reference = "webshop:vip:order-1", Reason = "Test" };
        var first = (ObjectResult)await this._controller.GrantVipAsync("buyer", request, CancellationToken.None).ConfigureAwait(false);
        var firstGrant = (VipMembershipGrantInfo)first.Value!;
        var replay = (OkObjectResult)await this._controller.GrantVipAsync("buyer", request, CancellationToken.None).ConfigureAwait(false);

        Assert.That(first.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That(((VipMembershipGrantInfo)replay.Value!).Id, Is.EqualTo(firstGrant.Id));
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(this._gameConfiguration);
        var account = await context.GetAccountByLoginNameAsync("buyer").ConfigureAwait(false);
        Assert.That(account!.VipUntil, Is.EqualTo(firstGrant.VipUntil));
        Assert.That(account.VipUntil, Is.GreaterThan(DateTime.UtcNow.AddDays(29)));
    }

    /// <summary>
    /// A VIP reference cannot be reused for another duration or account.
    /// </summary>
    [Test]
    public async Task VipMembershipReferenceMismatchIsConflictAsync()
    {
        await this._controller.GrantVipAsync("buyer", new VipMembershipGrantRequest { DurationDays = 30, Reference = "webshop:vip:order-1" }, CancellationToken.None).ConfigureAwait(false);

        Assert.That(
            await this._controller.GrantVipAsync("buyer", new VipMembershipGrantRequest { DurationDays = 60, Reference = "webshop:vip:order-1" }, CancellationToken.None).ConfigureAwait(false),
            Is.InstanceOf<ConflictResult>());
        Assert.That(
            await this._controller.GrantVipAsync("other", new VipMembershipGrantRequest { DurationDays = 30, Reference = "webshop:vip:order-1" }, CancellationToken.None).ConfigureAwait(false),
            Is.InstanceOf<ConflictResult>());
    }

    private static CashShopCoinGrantRequest CreateRequest(int amount, string? reference)
    {
        return new CashShopCoinGrantRequest
        {
            CoinType = CashShopCoinType.WCoinC,
            Amount = amount,
            Reason = "Test",
            Reference = reference,
        };
    }

    private static CashShopItemGrantRequest CreateItemGrantRequest(int packageSequence, int? priceSequence, string reference)
        => new()
        {
            PackageSequence = packageSequence,
            PriceSequence = priceSequence,
            Reference = reference,
            Reason = "Web shop test",
        };

    private static GameConfiguration CreateGameConfiguration()
    {
        var configuration = new MUnique.OpenMU.Persistence.BasicModel.GameConfiguration();
        var cashShop = new MUnique.OpenMU.Persistence.BasicModel.CashShopConfiguration();
        configuration.CashShopConfiguration = cashShop;

        var options = new MUnique.OpenMU.Persistence.BasicModel.CashShopPackage
        {
            PackageSequence = 10,
            IsForSale = true,
        };
        options.Products.Add(new MUnique.OpenMU.Persistence.BasicModel.CashShopProduct { ProductSequence = 1001, PriceSequence = 101 });
        options.Products.Add(new MUnique.OpenMU.Persistence.BasicModel.CashShopProduct { ProductSequence = 1002, PriceSequence = 102 });
        cashShop.Packages.Add(options);

        var bundle = new MUnique.OpenMU.Persistence.BasicModel.CashShopPackage
        {
            PackageSequence = 20,
            IsForSale = true,
            IsBundle = true,
        };
        bundle.Products.Add(new MUnique.OpenMU.Persistence.BasicModel.CashShopProduct { ProductSequence = 2001, PriceSequence = 201 });
        bundle.Products.Add(new MUnique.OpenMU.Persistence.BasicModel.CashShopProduct { ProductSequence = 2002, PriceSequence = 202 });
        cashShop.Packages.Add(bundle);
        return configuration;
    }
}
