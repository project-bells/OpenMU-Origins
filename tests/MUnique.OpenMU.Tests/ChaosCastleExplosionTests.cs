// <copyright file="ChaosCastleExplosionTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests the deterministic geometry of Chaos Castle monster explosions.
/// </summary>
[TestFixture]
public class ChaosCastleExplosionTests
{
    /// <summary>
    /// Tests that an explosion pushes a nearby player away on both axes.
    /// </summary>
    [Test]
    public void GetTargetPosition_PlayerInRange_MovesAwayFromExplosion()
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(random => random.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(2);
        randomizer.Setup(random => random.NextRandomBool()).Returns(true);

        var target = ChaosCastleExplosion.GetTargetPosition(new Point(12, 12), 2, 1, 1, randomizer.Object);

        Assert.That(target, Is.EqualTo(new Point(14, 14)));
    }

    /// <summary>
    /// Tests that explosion knockback cannot wrap coordinates past the map boundary.
    /// </summary>
    [Test]
    public void GetTargetPosition_NearMaximumBoundary_ClampsCoordinates()
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(random => random.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(3);
        randomizer.Setup(random => random.NextRandomBool()).Returns(true);

        var target = ChaosCastleExplosion.GetTargetPosition(new Point(254, 254), 1, 1, 1, randomizer.Object);

        Assert.That(target, Is.EqualTo(new Point(byte.MaxValue, byte.MaxValue)));
    }
}
