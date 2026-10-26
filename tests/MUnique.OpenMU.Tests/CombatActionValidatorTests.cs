// <copyright file="CombatActionValidatorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.Pathfinding;
using NUnit.Framework;

/// <summary>
/// Tests the server-authoritative combat range rules.
/// </summary>
[TestFixture]
public class CombatActionValidatorTests
{
    /// <summary>
    /// Verifies the edge of the configured basic attack range.
    /// </summary>
    [Test]
    public void IsWithinRange_OnBoundary_ReturnsTrue()
        => Assert.That(CombatActionValidator.IsWithinRange(new Point(10, 10), new Point(13, 12), 3), Is.True);

    /// <summary>
    /// Verifies that an impossible melee distance is rejected.
    /// </summary>
    [Test]
    public void IsWithinRange_OutsideBoundary_ReturnsFalse()
        => Assert.That(CombatActionValidator.IsWithinRange(new Point(10, 10), new Point(14, 10), 3), Is.False);

    /// <summary>
    /// Verifies that caster-centered skills ignore the untrusted visual target point.
    /// </summary>
    [Test]
    public void IsAreaTargetWithinRange_CasterCenteredSkill_ReturnsTrue()
        => Assert.That(
            CombatActionValidator.IsAreaTargetWithinRange(new Point(10, 10), new Point(200, 200), new Skill { Range = 3 }, 2),
            Is.True);

    /// <summary>
    /// Verifies that a client-targeted area cannot be placed beyond the skill range and tolerance.
    /// </summary>
    [Test]
    public void IsAreaTargetWithinRange_TargetAreaOutsideRange_ReturnsFalse()
    {
        var skill = new Skill { Range = 4, AreaSkillSettings = new AreaSkillSettings { UseTargetAreaFilter = true } };
        Assert.That(CombatActionValidator.IsAreaTargetWithinRange(new Point(10, 10), new Point(17, 10), skill, 2), Is.False);
    }
}
