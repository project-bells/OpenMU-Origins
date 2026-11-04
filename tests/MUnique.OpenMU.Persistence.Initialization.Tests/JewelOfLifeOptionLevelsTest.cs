// <copyright file="JewelOfLifeOptionLevelsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the option levels which can be upgraded by Jewel of Life.
/// </summary>
[TestFixture]
internal class JewelOfLifeOptionLevelsTest
{
    /// <summary>
    /// Tests that a newly initialized season 6 database contains option levels up to seven.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsSevenJewelOfLifeOptionLevelsAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        AssertMaximumOptionLevel(gameConfiguration);
    }

    /// <summary>
    /// Tests that the update extends an old database and remains stable when applied repeatedly.
    /// </summary>
    [Test]
    public async Task UpdateExtendsJewelOfLifeOptionsIdempotentlyAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        foreach (var option in GetUpgradeableOptions(gameConfiguration))
        {
            foreach (var level in option.LevelDependentOptions.Where(level => level.Level > 4).ToList())
            {
                option.LevelDependentOptions.Remove(level);
            }
        }

        var update = new ExtendJewelOfLifeOptionsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        AssertMaximumOptionLevel(gameConfiguration);
    }

    private static void AssertMaximumOptionLevel(GameConfiguration gameConfiguration)
    {
        var options = GetUpgradeableOptions(gameConfiguration).ToList();
        Assert.That(options, Is.Not.Empty);
        Assert.That(options, Has.All.Matches<IncreasableItemOption>(option =>
            option.LevelDependentOptions.Max(level => level.Level) == 7));
    }

    private static IEnumerable<IncreasableItemOption> GetUpgradeableOptions(GameConfiguration gameConfiguration)
    {
        return gameConfiguration.ItemOptions
            .SelectMany(definition => definition.PossibleOptions)
            .Where(option => option.OptionType == ItemOptionTypes.Option && option.LevelDependentOptions.Any());
    }

    private static async Task<(IContext Context, GameConfiguration GameConfiguration)> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        return (context, gameConfiguration);
    }
}
