// <copyright file="IllusionTempleAvailabilityTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests that incomplete Illusion Temple gameplay is not exposed to players.
/// </summary>
[TestFixture]
internal class IllusionTempleAvailabilityTests
{
    private static readonly int[] TicketPackageSequences = [104, 105];

    /// <summary>
    /// Ensures that a fresh Season 6 database doesn't offer tickets for the incomplete event.
    /// </summary>
    [Test]
    public async Task FreshDatabaseDoesNotOfferIllusionTempleTicketsAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        AssertIllusionTempleTicketsDisabled(configuration);
    }

    /// <summary>
    /// Ensures that the update removes ticket acquisition from an existing database idempotently.
    /// </summary>
    [Test]
    public async Task UpdateDisablesIllusionTempleTicketsIdempotentlyAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var chaosGoblin = configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine);
        var obsoleteCrafting = context.CreateNew<ItemCrafting>();
        obsoleteCrafting.Number = 37;
        chaosGoblin.ItemCraftings.Add(obsoleteCrafting);
        foreach (var package in configuration.CashShopConfiguration!.Packages.Where(package => TicketPackageSequences.Contains(package.PackageSequence)))
        {
            package.IsForSale = true;
            package.IsGiftable = true;
        }

        var update = new DisableIncompleteIllusionTempleUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        AssertIllusionTempleTicketsDisabled(configuration);
    }

    private static void AssertIllusionTempleTicketsDisabled(GameConfiguration configuration)
    {
        var chaosGoblin = configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine);
        Assert.Multiple(() =>
        {
            Assert.That(chaosGoblin.ItemCraftings, Has.None.Matches<ItemCrafting>(crafting => crafting.Number == 37));
            Assert.That(
                configuration.CashShopConfiguration!.Packages.Where(package => TicketPackageSequences.Contains(package.PackageSequence)),
                Has.All.Matches<CashShopPackage>(package => !package.IsForSale && !package.IsGiftable));
        });
    }

    private static async Task<(IContext Context, GameConfiguration GameConfiguration)> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var context = contextProvider.CreateNewContext();
        return (context, (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First());
    }
}
