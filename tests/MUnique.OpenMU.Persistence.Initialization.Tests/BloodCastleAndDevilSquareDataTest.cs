// <copyright file="BloodCastleAndDevilSquareDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the entrance configuration of Blood Castle and Devil Square.
/// </summary>
[TestFixture]
internal class BloodCastleAndDevilSquareDataTest
{
    private static readonly (byte Level, short Map, short Minimum, short Maximum, short SpecialMinimum, short SpecialMaximum, bool MasterOnly)[] BloodCastleLevels =
    {
        (1, 11, 15, 80, 10, 60, false),
        (2, 12, 81, 130, 61, 110, false),
        (3, 13, 131, 180, 111, 160, false),
        (4, 14, 181, 230, 161, 210, false),
        (5, 15, 231, 280, 211, 260, false),
        (6, 16, 281, 330, 261, 310, false),
        (7, 17, 331, 400, 311, 400, false),
        (8, 52, 331, 400, 0, 400, true),
    };

    private static readonly (byte Level, short Map, short Minimum, short Maximum, short SpecialMinimum, short SpecialMaximum, bool MasterOnly)[] DevilSquareLevels =
    {
        (1, 9, 15, 130, 10, 110, false),
        (2, 9, 131, 180, 111, 160, false),
        (3, 9, 181, 230, 161, 210, false),
        (4, 9, 231, 280, 211, 260, false),
        (5, 32, 281, 330, 261, 310, false),
        (6, 32, 331, 400, 311, 400, false),
        (7, 32, 0, 400, 0, 400, true),
    };

    private static readonly short[][] DevilSquareMonsterNumbers =
    {
        [17, 15, 5, 13, 8, 36, 18],
        [10, 39, 34, 41, 40, 35, 49],
        [41, 37, 35, 180, 64, 65, 67],
        [64, 65, 60, 294, 57, 70, 66],
        [294, 60, 71, 190, 61, 73, 291],
        [290, 57, 70, 293, 74, 292, 197],
        [434, 435, 438, 439, 436, 437, 440],
    };

    private static readonly short[] BloodCastleSpiritSorcererNumbers = { 89, 95, 112, 118, 124, 130, 143, 433 };

    private static readonly int[] BloodCastleGateHealth = { 150_000, 205_000, 260_000, 325_000, 400_000, 480_000, 565_000, 650_000 };

    private static readonly int[] BloodCastleStatueHealth = { 65_000, 105_000, 145_000, 185_000, 225_000, 265_000, 305_000, 345_000 };

    /// <summary>
    /// Tests all configured Blood Castle entrance brackets, maps, and ticket levels.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsAllBloodCastleEntranceDefinitionsAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);

        AssertEntranceDefinitions(
            configuration,
            MiniGameType.BloodCastle,
            BloodCastleLevels,
            ticketGroup: 13,
            ticketNumber: 18);
    }

    /// <summary>
    /// Tests all configured Devil Square entrance brackets, maps, and ticket levels.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsAllDevilSquareEntranceDefinitionsAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);

        AssertEntranceDefinitions(
            configuration,
            MiniGameType.DevilSquare,
            DevilSquareLevels,
            ticketGroup: 14,
            ticketNumber: 19);
    }

    /// <summary>
    /// Tests the Blood Castle monster spawn configuration of every game level.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsValidBloodCastleMonsterSpawnsAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.BloodCastle)
            .OrderBy(definition => definition.GameLevel)
            .ToList();

        Assert.That(definitions, Has.Count.EqualTo(BloodCastleLevels.Length));
        foreach (var definition in definitions)
        {
            var spawns = definition.Entrance!.Map!.MonsterSpawns;
            Assert.That(spawns, Has.Count.EqualTo(97), $"Blood Castle {definition.GameLevel} spawn count");
            Assert.That(spawns.Count(spawn => spawn.SpawnTrigger == SpawnTrigger.AutomaticDuringEvent), Is.EqualTo(95), $"Blood Castle {definition.GameLevel} regular spawn count");
            Assert.That(spawns.Where(spawn => spawn.SpawnTrigger == SpawnTrigger.AutomaticDuringEvent).Select(spawn => spawn.Quantity), Is.All.EqualTo(1), $"Blood Castle {definition.GameLevel} regular quantities");
            Assert.That(spawns.Select(spawn => spawn.MonsterDefinition).Distinct().ToList(), Has.Count.EqualTo(8), $"Blood Castle {definition.GameLevel} monster types");
            foreach (var spawn in spawns)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(spawn.MonsterDefinition, Is.Not.Null.And.AnyOf(configuration.Monsters.ToArray()), $"Blood Castle {definition.GameLevel} monster definition");
                    Assert.That(spawn.GameMap, Is.SameAs(definition.Entrance.Map), $"Blood Castle {definition.GameLevel} map reference");
                    Assert.That(spawn.X1, Is.LessThanOrEqualTo(spawn.X2), $"Blood Castle {definition.GameLevel} x coordinates");
                    Assert.That(spawn.Y1, Is.LessThanOrEqualTo(spawn.Y2), $"Blood Castle {definition.GameLevel} y coordinates");
                    Assert.That(spawn.Quantity, Is.Positive, $"Blood Castle {definition.GameLevel} quantity");
                    Assert.That(spawn.SpawnTrigger, Is.AnyOf(SpawnTrigger.Automatic, SpawnTrigger.AutomaticDuringEvent, SpawnTrigger.OnceAtEventStart), $"Blood Castle {definition.GameLevel} trigger");
                });
            }
        }
    }

    /// <summary>
    /// Tests the Devil Square monsters and wave configuration of every game level.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsValidDevilSquareMonsterWavesAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.DevilSquare)
            .OrderBy(definition => definition.GameLevel)
            .ToList();

        Assert.That(definitions, Has.Count.EqualTo(DevilSquareLevels.Length));
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var spawns = definition.Entrance!.Map!.MonsterSpawns.OrderBy(spawn => spawn.WaveNumber).ThenBy(spawn => spawn.MonsterDefinition!.Number).ToList();
            var expectedMonsters = DevilSquareMonsterNumbers[index];

            Assert.Multiple(() =>
            {
                Assert.That(spawns.Select(spawn => spawn.MonsterDefinition!.Number), Is.EquivalentTo(expectedMonsters), $"Devil Square {definition.GameLevel} monsters");
                Assert.That(spawns.Where(spawn => spawn.WaveNumber != 10).Select(spawn => spawn.Quantity), Is.All.EqualTo(35), $"Devil Square {definition.GameLevel} regular quantities");
                Assert.That(spawns.Where(spawn => spawn.WaveNumber == 10).Select(spawn => spawn.Quantity), Is.EqualTo(new short[] { 5 }), $"Devil Square {definition.GameLevel} boss quantity");
                Assert.That(definition.SpawnWaves.Select(wave => (wave.WaveNumber, wave.StartTime, wave.EndTime)), Is.EquivalentTo(new[]
                {
                    ((byte)1, TimeSpan.Zero, TimeSpan.FromMinutes(7)),
                    ((byte)2, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(14)),
                    ((byte)3, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(20)),
                    ((byte)10, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(20)),
                }), $"Devil Square {definition.GameLevel} wave timing");
            });

            foreach (var spawn in spawns)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(spawn.MonsterDefinition, Is.Not.Null.And.AnyOf(configuration.Monsters.ToArray()), $"Devil Square {definition.GameLevel} monster definition");
                    Assert.That(spawn.GameMap, Is.SameAs(definition.Entrance.Map), $"Devil Square {definition.GameLevel} map reference");
                    Assert.That(spawn.X1, Is.LessThanOrEqualTo(spawn.X2), $"Devil Square {definition.GameLevel} x coordinates");
                    Assert.That(spawn.Y1, Is.LessThanOrEqualTo(spawn.Y2), $"Devil Square {definition.GameLevel} y coordinates");
                    Assert.That(spawn.WaveNumber, Is.AnyOf((byte)1, (byte)2, (byte)3, (byte)10), $"Devil Square {definition.GameLevel} wave number");
                    Assert.That(spawn.SpawnTrigger, spawn.WaveNumber == 10 ? Is.EqualTo(SpawnTrigger.OnceAtWaveStart) : Is.EqualTo(SpawnTrigger.AutomaticDuringWave), $"Devil Square {definition.GameLevel} trigger");
                });
            }
        }
    }

    /// <summary>
    /// Tests the configured Blood Castle gate, statue, and Archangel weapon quest sequence.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsBloodCastleQuestSequenceAsync()
    {
        var configuration = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        var questItem = configuration.Items.Single(item => item is { Group: 13, Number: 19 });
        var statue = configuration.Monsters.Single(monster => monster.Number == 132);
        var questDrop = statue.DropItemGroups.Single();
        Assert.Multiple(() =>
        {
            Assert.That(statue.NumberOfMaximumItemDrops, Is.EqualTo(1));
            Assert.That(questDrop.Monster, Is.SameAs(statue));
            Assert.That(questDrop.Chance, Is.EqualTo(1));
            Assert.That(questDrop.PossibleItems, Is.EqualTo(new[] { questItem }));
        });

        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == MiniGameType.BloodCastle)
            .OrderBy(definition => definition.GameLevel)
            .ToList();
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var events = definition.ChangeEvents.OrderBy(changeEvent => changeEvent.Index).ToList();
            var gateSpawn = definition.Entrance!.Map!.MonsterSpawns.Single(spawn => spawn.MonsterDefinition?.Number == 131);

            Assert.That(events.Select(changeEvent => changeEvent.Index), Is.EqualTo(new[] { 0, 1, 2, 3 }), $"Blood Castle {definition.GameLevel} event order");
            Assert.Multiple(() =>
            {
                Assert.That(gateSpawn.SpawnTrigger, Is.EqualTo(SpawnTrigger.OnceAtEventStart), $"Blood Castle {definition.GameLevel} gate trigger");
                Assert.That(gateSpawn.MaximumHealthOverride, Is.EqualTo(BloodCastleGateHealth[index]), $"Blood Castle {definition.GameLevel} gate health");

                Assert.That(events[0].NumberOfKills, Is.Zero, $"Blood Castle {definition.GameLevel} initial event");
                Assert.That(events[0].TerrainChanges.Select(TerrainChangeValues), Is.EquivalentTo(new[]
                {
                    (13, 15, 15, 23, TerrainAttributeType.Blocked),
                    (11, 78, 25, 89, TerrainAttributeType.Blocked),
                    (8, 78, 11, 83, TerrainAttributeType.Blocked),
                }), $"Blood Castle {definition.GameLevel} initial terrain");

                Assert.That(events[1].Target, Is.EqualTo(KillTarget.AnyMonster), $"Blood Castle {definition.GameLevel} bridge target");
                Assert.That(events[1].NumberOfKills, Is.EqualTo(40), $"Blood Castle {definition.GameLevel} bridge kills");
                Assert.That(events[1].MultiplyKillsByPlayers, Is.True, $"Blood Castle {definition.GameLevel} bridge scaling");
                Assert.That(events[1].TerrainChanges.Select(TerrainChangeValues), Is.EqualTo(new[] { (13, 70, 15, 75, TerrainAttributeType.NoGround) }), $"Blood Castle {definition.GameLevel} bridge terrain");

                Assert.That(events[2].Target, Is.EqualTo(KillTarget.Specific), $"Blood Castle {definition.GameLevel} gate target");
                Assert.That(events[2].TargetDefinition?.Number, Is.EqualTo(131), $"Blood Castle {definition.GameLevel} gate definition");
                Assert.That(events[2].NumberOfKills, Is.EqualTo(1), $"Blood Castle {definition.GameLevel} gate kills");
                Assert.That(events[2].TerrainChanges.Select(TerrainChangeValues), Is.EqualTo(new[] { (13, 76, 15, 79, TerrainAttributeType.Blocked) }), $"Blood Castle {definition.GameLevel} gate terrain");

                Assert.That(events[3].Target, Is.EqualTo(KillTarget.Specific), $"Blood Castle {definition.GameLevel} statue prerequisite");
                Assert.That(events[3].TargetDefinition?.Number, Is.EqualTo(BloodCastleSpiritSorcererNumbers[index]), $"Blood Castle {definition.GameLevel} Spirit Sorcerer");
                Assert.That(events[3].NumberOfKills, Is.EqualTo(2), $"Blood Castle {definition.GameLevel} Spirit Sorcerer kills");
                Assert.That(events[3].MultiplyKillsByPlayers, Is.True, $"Blood Castle {definition.GameLevel} Spirit Sorcerer scaling");
                Assert.That(events[3].SpawnArea?.MonsterDefinition, Is.SameAs(statue), $"Blood Castle {definition.GameLevel} statue definition");
                Assert.That(events[3].SpawnArea?.MaximumHealthOverride, Is.EqualTo(BloodCastleStatueHealth[index]), $"Blood Castle {definition.GameLevel} statue health");
                Assert.That(events[3].SpawnArea is { X1: 14, Y1: 95, X2: 14, Y2: 95, Quantity: 1, SpawnTrigger: SpawnTrigger.OnceAtWaveStart }, Is.True, $"Blood Castle {definition.GameLevel} statue spawn");
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

    private static (int StartX, int StartY, int EndX, int EndY, TerrainAttributeType Attribute) TerrainChangeValues(MiniGameTerrainChange change)
    {
        Assert.Multiple(() =>
        {
            Assert.That(change.SetTerrainAttribute, Is.False);
            Assert.That(change.IsClientUpdateRequired, Is.True);
        });

        return (change.StartX, change.StartY, change.EndX, change.EndY, change.TerrainAttribute);
    }

    private static void AssertEntranceDefinitions(
        GameConfiguration configuration,
        MiniGameType gameType,
        IEnumerable<(byte Level, short Map, short Minimum, short Maximum, short SpecialMinimum, short SpecialMaximum, bool MasterOnly)> expectedLevels,
        byte ticketGroup,
        short ticketNumber)
    {
        var definitions = configuration.MiniGameDefinitions
            .Where(definition => definition.Type == gameType)
            .OrderBy(definition => definition.GameLevel)
            .ToList();
        var expected = expectedLevels.ToList();

        Assert.That(definitions, Has.Count.EqualTo(expected.Count));
        for (var index = 0; index < expected.Count; index++)
        {
            var definition = definitions[index];
            var level = expected[index];
            Assert.Multiple(() =>
            {
                Assert.That(definition.GameLevel, Is.EqualTo(level.Level), $"{gameType} level");
                Assert.That(definition.MinimumCharacterLevel, Is.EqualTo(level.Minimum), $"{gameType} {level.Level} normal minimum");
                Assert.That(definition.MaximumCharacterLevel, Is.EqualTo(level.Maximum), $"{gameType} {level.Level} normal maximum");
                Assert.That(definition.MinimumSpecialCharacterLevel, Is.EqualTo(level.SpecialMinimum), $"{gameType} {level.Level} special minimum");
                Assert.That(definition.MaximumSpecialCharacterLevel, Is.EqualTo(level.SpecialMaximum), $"{gameType} {level.Level} special maximum");
                Assert.That(definition.RequiresMasterClass, Is.EqualTo(level.MasterOnly), $"{gameType} {level.Level} master restriction");
                Assert.That(definition.Entrance?.Map?.Number, Is.EqualTo(level.Map), $"{gameType} {level.Level} map");
                Assert.That(definition.TicketItem, Is.SameAs(configuration.Items.Single(item => item is { Group: var group, Number: var number } && group == ticketGroup && number == ticketNumber)));
                Assert.That(definition.TicketItemLevel, Is.EqualTo(level.Level), $"{gameType} {level.Level} ticket level");
                Assert.That(definition.AllowParty, Is.True, $"{gameType} {level.Level} party policy");
                Assert.That(definition.MapCreationPolicy, Is.EqualTo(MiniGameMapCreationPolicy.Shared));
            });
        }
    }
}
