// <copyright file="KanturuParityDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Tests the Season 6 Kanturu Maya and Nightmare encounter data.
/// </summary>
[TestFixture]
internal class KanturuParityDataTest
{
    private const byte KanturuEventMapNumber = 39;

    private static readonly (byte Wave, short Monster, byte X1, byte X2, byte Y1, byte Y2, short Quantity)[] ExpectedWaves =
    {
        (0, 364, 188, 188, 110, 110, 1),
        (1, 354, 175, 215, 58, 86, 30),
        (1, 360, 175, 215, 58, 86, 10),
        (2, 362, 202, 202, 83, 83, 1),
        (3, 354, 175, 215, 58, 86, 30),
        (3, 360, 175, 215, 58, 86, 10),
        (4, 363, 189, 189, 82, 82, 1),
        (5, 359, 175, 215, 58, 86, 10),
        (5, 360, 175, 215, 58, 86, 10),
        (6, 362, 202, 202, 83, 83, 1),
        (6, 363, 189, 189, 82, 82, 1),
        (7, 357, 75, 88, 97, 137, 15),
        (7, 358, 75, 88, 97, 137, 15),
        (7, 360, 75, 88, 97, 137, 15),
        (8, 361, 78, 78, 143, 143, 1),
        (9, 360, 77, 81, 98, 102, 7),
        (10, 360, 76, 80, 122, 126, 7),
        (11, 360, 76, 80, 139, 143, 7),
    };

    /// <summary>
    /// Tests all Maya, Nightmare, and Nightmare summon wave spawn areas.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsExactKanturuWaveCoordinatesAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;
        var map = configuration.Maps.Single(map => map.Number == KanturuEventMapNumber);
        var actual = map.MonsterSpawns
            .Where(spawn => spawn.SpawnTrigger == SpawnTrigger.OnceAtWaveStart)
            .OrderBy(spawn => spawn.WaveNumber)
            .ThenBy(spawn => spawn.MonsterDefinition!.Number)
            .Select(spawn => (spawn.WaveNumber, spawn.MonsterDefinition!.Number, spawn.X1, spawn.X2, spawn.Y1, spawn.Y2, spawn.Quantity))
            .ToList();

        Assert.That(actual, Is.EqualTo(ExpectedWaves));
    }

    /// <summary>
    /// Tests the Maya and Nightmare combat definitions used by the encounter.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsExactKanturuBossDefinitionsAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;
        var bosses = configuration.Monsters.Where(monster => monster.Number is >= 361 and <= 364).ToDictionary(monster => monster.Number);

        Assert.Multiple(() =>
        {
            Assert.That(bosses, Has.Count.EqualTo(4));
            AssertBoss(bosses[361], (short)SkillNumber.Decay, 1_500_000, 5);
            AssertBoss(bosses[362], (short)SkillNumber.IceStorm, 400_000, 3);
            AssertBoss(bosses[363], (short)SkillNumber.IceStorm, 350_000, 3);
            AssertBoss(bosses[364], null, 5_000_000, 7);
        });
    }

    private static void AssertBoss(MonsterDefinition boss, short? attackSkill, int maximumHealth, byte maximumDrops)
    {
        Assert.That(boss.AttackSkill?.Number, Is.EqualTo(attackSkill), $"monster {boss.Number} attack skill");
        Assert.That(boss.Attributes.Single(attribute => attribute.AttributeDefinition?.Id == Stats.MaximumHealth.Id).Value, Is.EqualTo(maximumHealth), $"monster {boss.Number} health");
        Assert.That(boss.NumberOfMaximumItemDrops, Is.EqualTo(maximumDrops), $"monster {boss.Number} maximum drops");
        Assert.That(boss.RespawnDelay, Is.EqualTo(TimeSpan.Zero), $"monster {boss.Number} respawn delay");
    }
}
