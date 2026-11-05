// <copyright file="ChaosCastleDropGeneratorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Tests the configured Chaos Castle monster jewel distribution.
/// </summary>
[TestFixture]
public class ChaosCastleDropGeneratorTests
{
    /// <summary>
    /// Tests that every configured group is assigned to one distinct monster kill and generated exactly once.
    /// </summary>
    [Test]
    public async Task GenerateItemDropsAsync_ConfiguredGroups_DropAtDistinctKillsExactlyOnce()
    {
        var firstGroup = new Mock<DropItemGroup>().Object;
        var secondGroup = new Mock<DropItemGroup>().Object;
        var firstItem = new Mock<Item>().Object;
        var secondItem = new Mock<Item>().Object;
        var defaultGenerator = new Mock<IDropGenerator>();
        defaultGenerator.Setup(generator => generator.GenerateItemDrop(firstGroup)).Returns(firstItem);
        defaultGenerator.Setup(generator => generator.GenerateItemDrop(secondGroup)).Returns(secondItem);
        var gameContext = new Mock<IGameContext>();
        gameContext.Setup(context => context.DropGenerator).Returns(defaultGenerator.Object);
        var randomizer = new Mock<IRandomizer>();
        randomizer.SetupSequence(random => random.NextInt(1, 5)).Returns(2).Returns(4);
        var generator = new ChaosCastleDropGenerator(gameContext.Object, [firstGroup, secondGroup], 4, randomizer.Object);

        var drops = new List<Item[]>();
        for (var kill = 0; kill < 4; kill++)
        {
            var (items, _) = await generator.GenerateItemDropsAsync(null!, 0, null!).ConfigureAwait(false);
            drops.Add(items.ToArray());
        }

        Assert.Multiple(() =>
        {
            Assert.That(drops[0], Is.Empty);
            Assert.That(drops[1], Is.EqualTo(new[] { firstItem }));
            Assert.That(drops[2], Is.Empty);
            Assert.That(drops[3], Is.EqualTo(new[] { secondItem }));
        });
        defaultGenerator.Verify(generator => generator.GenerateItemDrop(It.IsAny<DropItemGroup>()), Times.Exactly(2));
    }
}
