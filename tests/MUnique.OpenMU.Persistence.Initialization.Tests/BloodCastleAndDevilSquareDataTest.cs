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
