// <copyright file="ChaosCastleDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
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

    private static readonly (byte StartX, byte StartY, byte EndX, byte EndY)[][] CollapseStages =
    {
        [(0x17, 0x4B, 0x2C, 0x4C), (0x2B, 0x4D, 0x2C, 0x6C), (0x17, 0x6B, 0x2A, 0x6C), (0x17, 0x4D, 0x18, 0x6A)],
        [(0x19, 0x4D, 0x2A, 0x4E), (0x29, 0x4F, 0x2A, 0x6A), (0x19, 0x69, 0x28, 0x6A), (0x19, 0x4F, 0x1A, 0x68)],
        [(0x1B, 0x4F, 0x28, 0x50), (0x27, 0x51, 0x28, 0x68), (0x1B, 0x67, 0x26, 0x68), (0x1B, 0x51, 0x1C, 0x66)],
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

    /// <summary>
    /// Tests the initial safezone removal and all three configured floor-collapse stages of every level.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsAllChaosCastleFloorCollapseStagesAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.ChaosCastle)
            .OrderBy(definition => definition.GameLevel)
            .ToList();

        foreach (var definition in definitions)
        {
            var events = definition.ChangeEvents.OrderBy(changeEvent => changeEvent.Index).ToList();
            Assert.That(events, Has.Count.EqualTo(4), $"Chaos Castle {definition.GameLevel} change event count");
            Assert.Multiple(() =>
            {
                Assert.That(events.Select(changeEvent => changeEvent.NumberOfKills), Is.EqualTo(new short[] { 0, 60, 10, 10 }));
                Assert.That(events, Has.All.Matches<MiniGameChangeEvent>(changeEvent => changeEvent.Target == KillTarget.AnyObject));

                var safezoneChange = events[0].TerrainChanges.Single();
                Assert.That(TerrainValues(safezoneChange), Is.EqualTo((23, 75, 44, 108, false, true, TerrainAttributeType.Safezone)));

                for (var stageIndex = 0; stageIndex < CollapseStages.Length; stageIndex++)
                {
                    var actualAreas = events[stageIndex + 1].TerrainChanges
                        .Select(change => (change.StartX, change.StartY, change.EndX, change.EndY))
                        .ToList();
                    Assert.That(actualAreas, Is.EqualTo(CollapseStages[stageIndex]), $"Chaos Castle {definition.GameLevel} collapse stage {stageIndex + 1}");
                    Assert.That(events[stageIndex + 1].TerrainChanges, Has.All.Matches<MiniGameTerrainChange>(change => change.SetTerrainAttribute));
                    Assert.That(events[stageIndex + 1].TerrainChanges, Has.All.Matches<MiniGameTerrainChange>(change => !change.IsClientUpdateRequired));
                    Assert.That(events[stageIndex + 1].TerrainChanges, Has.All.Matches<MiniGameTerrainChange>(change => change.TerrainAttribute == TerrainAttributeType.NoGround));
                }
            });
        }
    }

    /// <summary>
    /// Tests the winner-only jewel and Ancient Item reward configuration of every level.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsAllChaosCastleWinnerRewardsAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.ChaosCastle)
            .OrderBy(definition => definition.GameLevel)
            .ToList();

        foreach (var definition in definitions)
        {
            var rewards = definition.Rewards.ToList();
            var jewelReward = rewards.Single(reward => reward.ItemReward?.ItemType == SpecialItemType.None);
            var ancientRewards = rewards.Where(reward => reward.ItemReward?.ItemType == SpecialItemType.Ancient).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(rewards, Has.Count.EqualTo(definition.GameLevel == 1 ? 1 : 2));
                AssertWinnerDrop(jewelReward, 1.0);
                Assert.That(jewelReward.ItemReward!.PossibleItems.Select(item => (item.Group, item.Number)), Is.EquivalentTo(new[]
                {
                    ((byte)12, (short)15),
                    ((byte)14, (short)13),
                    ((byte)14, (short)14),
                    ((byte)14, (short)16),
                    ((byte)14, (short)22),
                }));
                Assert.That(ancientRewards, Has.Count.EqualTo(definition.GameLevel == 1 ? 0 : 1));
                if (definition.GameLevel > 1)
                {
                    AssertWinnerDrop(ancientRewards.Single(), definition.GameLevel / 10.0);
                }
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

    private static (byte StartX, byte StartY, byte EndX, byte EndY, bool SetAttribute, bool ClientUpdate, TerrainAttributeType Attribute) TerrainValues(MiniGameTerrainChange change)
    {
        return (change.StartX, change.StartY, change.EndX, change.EndY, change.SetTerrainAttribute, change.IsClientUpdateRequired, change.TerrainAttribute);
    }

    private static void AssertWinnerDrop(MiniGameReward reward, double chance)
    {
        Assert.That(reward.RewardType, Is.EqualTo(MiniGameRewardType.ItemDrop));
        Assert.That(reward.RewardAmount, Is.EqualTo(1));
        Assert.That(reward.RequiredSuccess, Is.EqualTo(MiniGameSuccessFlags.Winner | MiniGameSuccessFlags.Alive));
        Assert.That(reward.ItemReward!.Chance, Is.EqualTo(chance).Within(0.0001));
    }
}
