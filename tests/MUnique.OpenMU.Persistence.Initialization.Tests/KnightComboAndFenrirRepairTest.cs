// <copyright file="KnightComboAndFenrirRepairTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Verifies the repair for the Knight combo and the Fenrir skill.
/// </summary>
[TestFixture]
internal class KnightComboAndFenrirRepairTest
{
    /// <summary>
    /// Tests that an existing damaged configuration is restored and repeated application is stable.
    /// </summary>
    [Test]
    public async Task RepairsExistingConfigurationIdempotentlyAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var bladeKnight = configuration.CharacterClasses.Single(c => c.Number == (byte)CharacterClassNumber.BladeKnight);
        bladeKnight.ComboDefinition = null;
        var fenrir = configuration.Items.Single(item => item is { Group: 13, Number: 37 });
        fenrir.Skill = null;

        var update = new RepairKnightComboAndFenrirSkillUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(bladeKnight.ComboDefinition, Is.Not.Null);
            Assert.That(bladeKnight.ComboDefinition!.MaximumCompletionTime, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(bladeKnight.ComboDefinition.Steps.Count(step => step.Order == 1), Is.EqualTo(5));
            Assert.That(bladeKnight.ComboDefinition.Steps.Count(step => step.Order == 2), Is.EqualTo(4));
            Assert.That(bladeKnight.ComboDefinition.Steps.Count(step => step.Order == 3 && step.IsFinalStep), Is.EqualTo(3));
            Assert.That(fenrir.Skill?.Number, Is.EqualTo((short)SkillNumber.PlasmaStorm));
        });
    }
}
