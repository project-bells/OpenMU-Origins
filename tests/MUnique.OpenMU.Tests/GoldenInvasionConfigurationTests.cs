// <copyright file="GoldenInvasionConfigurationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Globalization;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

/// <summary>
/// Tests the complete default Golden Invasion configuration.
/// </summary>
[TestFixture]
public class GoldenInvasionConfigurationTests
{
    private static readonly (ushort Monster, ushort Count, ushort[] Maps)[] ExpectedTiers =
    {
        (43, 10, [0]),
        (78, 10, [3]),
        (54, 10, [2]),
        (53, 10, [2]),
        (81, 10, [7]),
        (80, 10, [7]),
        (83, 10, [8]),
        (82, 10, [8]),
        (79, 10, [0, 3, 2]),
    };

    /// <summary>
    /// Tests every tier, its eligible maps, duration, messages, and hourly recurrence.
    /// </summary>
    [Test]
    public void DefaultConfigurationContainsAllGoldenTiers()
    {
        var configuration = (PeriodicInvasionConfiguration)new GoldenInvasionPlugIn().CreateDefaultConfig();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.TaskDuration, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(configuration.PreStartMessageDelay, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(configuration.StartMessage.GetTranslation(CultureInfo.InvariantCulture), Does.Contain("{mapName}"));
            Assert.That(configuration.EndMessage.GetTranslation(CultureInfo.InvariantCulture), Does.Contain("{mapName}"));
            Assert.That(configuration.Timetable, Is.EqualTo(Enumerable.Range(0, 24).Select(hour => new TimeOnly(hour, 0))));
            Assert.That(
                configuration.Mobs.Select(mob => (mob.MonsterId, mob.Count, mob.MapIds.ToArray())),
                Is.EqualTo(ExpectedTiers));
            Assert.That(configuration.Mobs, Has.All.Matches<InvasionSpawnConfiguration>(mob => !mob.IsSpawnOnAllMaps && !mob.AnnounceDeath && mob.X is null && mob.Y is null));
        });
    }
}
