// <copyright file="ChaosCastleDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the population and placement configuration of Chaos Castle.
/// </summary>
[TestFixture]
internal class ChaosCastleDataTest
{
    private static readonly (short Map, short MeleeMonster, short RangedMonster)[] Levels =
    {
        (18, 162, 163),
        (19, 164, 165),
        (20, 166, 167),
        (21, 168, 169),
        (22, 170, 171),
        (23, 172, 173),
        (53, 426, 427),
    };

    /// <summary>
    /// Tests player capacity, randomized entrance placement, and the 99-monster pool which fills the event to 100 objects.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsAllChaosCastlePopulationAndPlacementDataAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.ChaosCastle)
            .OrderBy(definition => definition.GameLevel)
            .ToList();

        Assert.That(definitions, Has.Count.EqualTo(Levels.Length));
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var expected = Levels[index];
            var entrance = definition.Entrance ?? throw new AssertionException($"Chaos Castle {definition.GameLevel} has no entrance.");
            var map = entrance.Map ?? throw new AssertionException($"Chaos Castle {definition.GameLevel} entrance has no map.");
            var spawns = map.MonsterSpawns.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(definition.GameLevel, Is.EqualTo(index + 1));
                Assert.That(definition.MaximumPlayerCount, Is.EqualTo(70));
                Assert.That(map.Number, Is.EqualTo(expected.Map));
                Assert.That(entrance.X1 < entrance.X2 || entrance.Y1 < entrance.Y2, Is.True, $"Chaos Castle {definition.GameLevel} entrance placement domain");
                Assert.That(spawns, Has.Count.EqualTo(99));
                Assert.That(spawns.Count(spawn => spawn.MonsterDefinition?.Number == expected.MeleeMonster), Is.EqualTo(50));
                Assert.That(spawns.Count(spawn => spawn.MonsterDefinition?.Number == expected.RangedMonster), Is.EqualTo(49));
                Assert.That(spawns, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.Quantity == 1));
                Assert.That(spawns, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.SpawnTrigger == SpawnTrigger.ManuallyForEvent));
                Assert.That(spawns, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.X1 == spawn.X2 && spawn.Y1 == spawn.Y2));
                Assert.That(spawns, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.X1 is >= 24 and <= 44 && spawn.Y1 is >= 75 and <= 106));
                Assert.That(spawns.Select(spawn => (spawn.X1, spawn.Y1)), Is.Unique);
            });
        }
    }

    private static async Task<GameConfiguration> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        return (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
    }
}
