// <copyright file="KantataAncientSetTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the excellent damage chance of the Kantata ancient set.
/// </summary>
[TestFixture]
internal class KantataAncientSetTest
{
    /// <summary>
    /// Tests that a newly initialized database contains the 10 percent chance.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsCorrectExcellentDamageChanceAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        AssertKantataExcellentDamageChance(gameConfiguration);
    }

    /// <summary>
    /// Tests that the update corrects the old value and remains stable when applied repeatedly.
    /// </summary>
    [Test]
    public async Task UpdateRepairsExcellentDamageChanceIdempotentlyAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var boost = GetKantataExcellentDamageChanceBoost(gameConfiguration);
        boost.ConstantValue.Value = 10.0f;
        boost.ConstantValue.AggregateType = AggregateType.Multiplicate;

        var update = new FixKantataExcellentDamageChanceUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        AssertKantataExcellentDamageChance(gameConfiguration);
    }

    private static void AssertKantataExcellentDamageChance(GameConfiguration gameConfiguration)
    {
        var constantValue = GetKantataExcellentDamageChanceBoost(gameConfiguration).ConstantValue;
        Assert.Multiple(() =>
        {
            Assert.That(constantValue.Value, Is.EqualTo(0.10f));
            Assert.That(constantValue.AggregateType, Is.EqualTo(AggregateType.AddRaw));
        });
    }

    private static PowerUpDefinitionValue GetKantataExcellentDamageChanceBoost(GameConfiguration gameConfiguration)
    {
        var kantata = gameConfiguration.ItemSetGroups.Single(set => set.Name.ValueInNeutralLanguage == "Kantata");
        return kantata.Options!.PossibleOptions.Single(option => option.PowerUpDefinition?.TargetAttribute == Stats.ExcellentDamageChance)
            .PowerUpDefinition!.Boost!;
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
