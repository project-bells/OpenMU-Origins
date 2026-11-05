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

    private static async Task<GameConfiguration> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        return (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
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
