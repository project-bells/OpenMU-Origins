// <copyright file="TalismanOfLuckTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the Talisman of Luck configuration.
/// </summary>
[TestFixture]
internal class TalismanOfLuckTest
{
    private static readonly int[] CraftingNumbers = [3, 4, 22, 23, 49, 50];

    /// <summary>
    /// Tests the fresh season 6 configuration.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsTalismanOfLuckAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        AssertTalismanConfiguration(configuration);
    }

    /// <summary>
    /// Tests that the update repairs an old configuration idempotently.
    /// </summary>
    [Test]
    public async Task UpdateAddsTalismanOfLuckIdempotentlyAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var talisman = configuration.Items.Single(item => item.Group == 14 && item.Number == 53);
        configuration.Items.Remove(talisman);
        foreach (var crafting in GetUpgradeCraftings(configuration))
        {
            var requirement = crafting.SimpleCraftingSettings!.RequiredItems.Single(item => item.PossibleItems.Contains(talisman));
            crafting.SimpleCraftingSettings.RequiredItems.Remove(requirement);
        }

        foreach (var package in configuration.CashShopConfiguration!.Packages.Where(package => package.PackageSequence is 98 or 99 or 389))
        {
            package.IsForSale = false;
            foreach (var product in package.Products)
            {
                product.ItemDefinition = null;
            }
        }

        var update = new AddTalismanOfLuckUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        AssertTalismanConfiguration(configuration);
    }

    private static void AssertTalismanConfiguration(GameConfiguration configuration)
    {
        var talisman = configuration.Items.Single(item => item.Group == 14 && item.Number == 53);
        foreach (var crafting in GetUpgradeCraftings(configuration))
        {
            var requirement = crafting.SimpleCraftingSettings!.RequiredItems.Single(item => item.PossibleItems.Contains(talisman));
            Assert.Multiple(() =>
            {
                Assert.That(requirement.MinimumAmount, Is.Zero);
                Assert.That(requirement.MaximumAmount, Is.EqualTo(1));
                Assert.That(requirement.AddPercentage, Is.EqualTo(10));
            });
        }

        Assert.That(configuration.CashShopConfiguration!.Packages
            .Where(package => package.PackageSequence is 98 or 99 or 389),
            Has.All.Matches<CashShopPackage>(package => package.IsForSale
                && package.Products.All(product => product.ItemDefinition == talisman)));
    }

    private static IEnumerable<ItemCrafting> GetUpgradeCraftings(GameConfiguration configuration)
    {
        return configuration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings
            .Where(crafting => CraftingNumbers.Contains(crafting.Number));
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
