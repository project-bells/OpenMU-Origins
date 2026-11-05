// <copyright file="ChaosCastlePvpDamageTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.Pet;

/// <summary>
/// Tests the PvP damage reductions of Chaos Castle.
/// </summary>
[TestFixture]
public class ChaosCastlePvpDamageTests
{
    private const int BaseDamage = 400;

    /// <summary>
    /// Tests the regular player and Dark Raven reductions with and without the shield system.
    /// </summary>
    [TestCase(false, false, 2)]
    [TestCase(true, false, 4)]
    [TestCase(false, true, 1)]
    [TestCase(true, true, 2)]
    public async Task DamageReductionMatchesClassicAndRavenRulesAsync(bool classicPvp, bool ravenAttack, int reductionDivisor)
    {
        var attacker = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var defender = await PlayerTestHelper.CreatePlayerAsync(attacker.GameContext).ConfigureAwait(false);
        ConfigureDeterministicDamage(attacker, defender, classicPvp);
        IAttacker attackingObject = ravenAttack ? new AttackerSurrogate(attacker, attacker.Attributes!) : attacker;

        var outsideDamage = await attackingObject.CalculateDamageAsync(defender, null, false).ConfigureAwait(false);
        var chaosCastle = CreateChaosCastleContext(attacker);
        await using var _ = chaosCastle.ConfigureAwait(false);
        attacker.CurrentMiniGame = chaosCastle;
        var chaosCastleDamage = await attackingObject.CalculateDamageAsync(defender, null, false).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(outsideDamage.TotalDamage, Is.GreaterThan(0));
            Assert.That(chaosCastleDamage.TotalDamage, Is.EqualTo(outsideDamage.TotalDamage / reductionDivisor));
        });
    }

    private static void ConfigureDeterministicDamage(Player attacker, Player defender, bool classicPvp)
    {
        attacker.Attributes!.AddElement(new SimpleElement(BaseDamage, AggregateType.AddRaw), Stats.MinimumPhysBaseDmg);
        attacker.Attributes.AddElement(new SimpleElement(BaseDamage, AggregateType.AddRaw), Stats.MaximumPhysBaseDmg);
        attacker.Attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.AttackRatePvp);
        attacker.Attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.AttackRatePvm);
        defender.Attributes![Stats.CurrentShield] = 0;
        if (classicPvp)
        {
            defender.Attributes[Stats.BaseStrength] = 0;
            defender.Attributes[Stats.BaseAgility] = 0;
            defender.Attributes[Stats.BaseVitality] = 0;
            defender.Attributes[Stats.BaseEnergy] = 0;
        }
    }

    private static MiniGameContext CreateChaosCastleContext(Player player)
    {
        var definitionMock = new Mock<MiniGameDefinition>();
        definitionMock.SetupGet(definition => definition.Rewards).Returns(new List<MiniGameReward>());
        definitionMock.SetupGet(definition => definition.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definitionMock.SetupGet(definition => definition.ChangeEvents).Returns(new List<MiniGameChangeEvent>());
        definitionMock.SetupGet(definition => definition.Entrance).Returns(new ExitGate { Map = player.GameContext.Configuration.Maps.First() });
        var definition = definitionMock.Object;
        definition.Type = MiniGameType.ChaosCastle;

        var mapInitializerMock = new Mock<IMapInitializer>();
        mapInitializerMock.Setup(initializer => initializer.CreateGameMap(It.IsAny<GameMapDefinition>()))
            .Returns<GameMapDefinition>(map => new GameMap(map, TimeSpan.FromMinutes(1), 16));
        return new MiniGameContext(new MiniGameMapKey(0, 0, string.Empty), definition, player.GameContext, mapInitializerMock.Object);
    }
}
