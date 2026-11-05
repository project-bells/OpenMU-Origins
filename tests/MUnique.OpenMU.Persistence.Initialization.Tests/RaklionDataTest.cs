// <copyright file="RaklionDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Raklion;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the data of the raklion event.
/// </summary>
[TestFixture]
internal class RaklionDataTest
{
    private const short HatcheryNumber = 58;
    private const short PoisonSkillNumber = 250;
    private const short IceStormSkillNumber = 251;
    private const short IceStrikeSkillNumber = 252;
    private const short FallSkillNumber = 253;
    private const short IronKnightNumber = 458;

    private static readonly (short Monster, byte X, byte Y)[] ExpectedEggs =
    {
        (460, 141, 27),
        (461, 141, 28),
        (462, 142, 28),
        (460, 146, 34),
        (462, 147, 34),
        (461, 146, 33),
        (460, 152, 31),
        (460, 152, 30),
        (461, 151, 30),
        (462, 146, 29),
        (462, 146, 30),
        (461, 147, 29),
        (460, 145, 24),
        (460, 144, 24),
        (461, 144, 25),
    };

    private static readonly (byte X, byte Y)[] ExpectedSummons =
    {
        (144, 25),
        (144, 29),
        (144, 32),
        (144, 35),
        (144, 38),
        (153, 24),
        (153, 28),
        (153, 32),
        (153, 35),
        (153, 38),
    };

    /// <summary>
    /// Tests that the monsters of the hatchery of a new database are spawned by the event.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsEventSpawnsAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = await CreateConfigurationAsync(contextProvider).ConfigureAwait(false);

        AssertEventSpawns(gameConfiguration);
        AssertSelupanSkills(gameConfiguration);
    }

    /// <summary>
    /// Tests that the update changes the automatic spawns of the hatchery of an existing database to the waves of the event
    /// and adds the skills of Selupan, and that applying it twice doesn't change anything.
    /// </summary>
    [Test]
    public async Task UpdateChangesSpawnsOfExistingDatabaseAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = await CreateConfigurationAsync(contextProvider).ConfigureAwait(false);
        foreach (var spawn in GetHatchery(gameConfiguration).MonsterSpawns)
        {
            spawn.SpawnTrigger = SpawnTrigger.Automatic;
            spawn.WaveNumber = 0;
        }

        foreach (var skill in gameConfiguration.Skills.Where(IsSelupanSkill).ToList())
        {
            gameConfiguration.Skills.Remove(skill);
        }

        var raklionUpdate = new AddRaklionEventUpdatePlugIn();
        for (var i = 0; i < 2; i++)
        {
            await raklionUpdate.ApplyUpdateAsync(contextProvider.CreateNewContext(), gameConfiguration).ConfigureAwait(false);
        }

        AssertEventSpawns(gameConfiguration);
        AssertSelupanSkills(gameConfiguration);
    }

    /// <summary>
    /// Tests that the iron knight of a new database uses the intelligence which lets it use its stab.
    /// </summary>
    [Test]
    public async Task NewDatabaseLetsIronKnightStabAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = await CreateConfigurationAsync(contextProvider).ConfigureAwait(false);

        Assert.That(GetIronKnight(gameConfiguration).IntelligenceTypeName, Is.EqualTo(typeof(IronKnightIntelligence).FullName));
    }

    /// <summary>
    /// Tests that the update sets the intelligence of the iron knight of an existing database,
    /// and that applying it twice doesn't change anything.
    /// </summary>
    [Test]
    public async Task UpdateLetsIronKnightOfExistingDatabaseStabAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = await CreateConfigurationAsync(contextProvider).ConfigureAwait(false);
        GetIronKnight(gameConfiguration).IntelligenceTypeName = null;

        var update = new AddIronKnightStabUpdatePlugIn();
        for (var i = 0; i < 2; i++)
        {
            await update.ApplyUpdateAsync(contextProvider.CreateNewContext(), gameConfiguration).ConfigureAwait(false);
        }

        Assert.That(GetIronKnight(gameConfiguration).IntelligenceTypeName, Is.EqualTo(typeof(IronKnightIntelligence).FullName));
    }

    /// <summary>
    /// Tests that the update keeps an intelligence, which was already configured for the iron knight.
    /// </summary>
    [Test]
    public async Task UpdateKeepsConfiguredIntelligenceOfIronKnightAsync()
    {
        const string customIntelligence = "Custom.Intelligence";
        var contextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = await CreateConfigurationAsync(contextProvider).ConfigureAwait(false);
        GetIronKnight(gameConfiguration).IntelligenceTypeName = customIntelligence;

        await new AddIronKnightStabUpdatePlugIn().ApplyUpdateAsync(contextProvider.CreateNewContext(), gameConfiguration).ConfigureAwait(false);

        Assert.That(GetIronKnight(gameConfiguration).IntelligenceTypeName, Is.EqualTo(customIntelligence));
    }

    private static async Task<GameConfiguration> CreateConfigurationAsync(InMemoryPersistenceContextProvider contextProvider)
    {
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = contextProvider.CreateNewContext();
        return (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    private static GameMapDefinition GetHatchery(GameConfiguration gameConfiguration)
    {
        return gameConfiguration.Maps.Single(map => map.Number == HatcheryNumber);
    }

    private static MonsterDefinition GetIronKnight(GameConfiguration gameConfiguration)
    {
        return gameConfiguration.Monsters.Single(monster => monster.Number == IronKnightNumber);
    }

    private static bool IsSelupanSkill(Skill skill)
    {
        return skill.Number is PoisonSkillNumber or IceStormSkillNumber or IceStrikeSkillNumber or FallSkillNumber;
    }

    /// <summary>
    /// Asserts that the attack skills of Selupan exist exactly once and carry their damage multiplier.
    /// Without the multiplier, the three skills would hit exactly the same.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    private static void AssertSelupanSkills(GameConfiguration gameConfiguration)
    {
        Assert.That(gameConfiguration.Skills.Where(IsSelupanSkill).ToList(), Has.Count.EqualTo(4));

        AssertMultiplier(PoisonSkillNumber, 2.0f);
        AssertMultiplier(IceStormSkillNumber, 2.2f);
        AssertMultiplier(IceStrikeSkillNumber, 2.3f);
        AssertMultiplier(FallSkillNumber, 2.5f);

        void AssertMultiplier(short number, float expected)
        {
            var skill = gameConfiguration.Skills.Single(s => s.Number == number);
            var relationship = skill.AttributeRelationships.Single(r => r.TargetAttribute?.Id == Stats.SkillFinalMultiplier.Id);

            Assert.That(relationship.InputOperand, Is.EqualTo(expected).Within(0.001f), $"Wrong multiplier of skill {number}.");
            Assert.That(relationship.InputOperator, Is.EqualTo(InputOperator.Maximum), $"The multiplier of skill {number} must not be multiplied by the skill multiplier of Selupan.");
            Assert.That(relationship.InputAttribute?.Id, Is.EqualTo(Stats.SkillMultiplier.Id));
        }
    }

    private static void AssertEventSpawns(GameConfiguration gameConfiguration)
    {
        var definition = new RaklionEventDefinition();
        var spawns = GetHatchery(gameConfiguration).MonsterSpawns;
        Assert.That(spawns.Where(spawn => spawn.SpawnTrigger != SpawnTrigger.OnceAtWaveStart), Is.Empty);

        var selupan = spawns.Where(spawn => spawn.WaveNumber == definition.SelupanWaveNumber).ToList();
        Assert.That(selupan, Has.Count.EqualTo(1));
        Assert.That(selupan[0].MonsterDefinition!.Number, Is.EqualTo(459));
        Assert.That((selupan[0].X1, selupan[0].Y1), Is.EqualTo((145, 31)));

        var eggs = spawns.Where(spawn => spawn.WaveNumber == definition.SpiderEggWaveNumber).ToList();
        Assert.That(
            eggs.Select(spawn => (spawn.MonsterDefinition!.Number, spawn.X1, spawn.Y1)),
            Is.EqualTo(ExpectedEggs));
        Assert.That(eggs, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.Quantity == 1 && spawn.X1 == spawn.X2 && spawn.Y1 == spawn.Y2));

        var summons = spawns.Where(spawn => spawn.WaveNumber == definition.SummonWaveNumber).ToList();
        Assert.That(
            summons.Select(spawn => (spawn.X1, spawn.Y1)),
            Is.EqualTo(ExpectedSummons));
        Assert.That(summons, Has.All.Matches<MonsterSpawnArea>(spawn => spawn.MonsterDefinition!.Number == 457 && spawn.Quantity == 1 && spawn.X1 == spawn.X2 && spawn.Y1 == spawn.Y2));
    }
}
