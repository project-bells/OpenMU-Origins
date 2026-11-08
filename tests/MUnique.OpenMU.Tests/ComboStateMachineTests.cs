// <copyright file="ComboStateMachineTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests the combo state machine across configuration generations.
/// </summary>
[TestFixture]
public class ComboStateMachineTests
{
    /// <summary>
    /// Verifies that a configuration reload doesn't leave combo states bound to old skill objects.
    /// </summary>
    [Test]
    public async Task ReloadedDefinitionUsesSkillsFromNewConfigurationAsync()
    {
        var definitionId = Guid.NewGuid();
        var oldDefinition = CreateDefinition(definitionId, out _);
        var newDefinition = CreateDefinition(definitionId, out var newSkills);

        _ = ComboStateMachine.Create(oldDefinition);
        var reloadedMachine = ComboStateMachine.Create(newDefinition);

        var firstStepCompleted = await reloadedMachine.RegisterSkillAsync(newSkills[0]).ConfigureAwait(false);
        var secondStepCompleted = await reloadedMachine.RegisterSkillAsync(newSkills[1]).ConfigureAwait(false);
        var finalStepCompleted = await reloadedMachine.RegisterSkillAsync(newSkills[2]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(firstStepCompleted, Is.False);
            Assert.That(secondStepCompleted, Is.False);
            Assert.That(finalStepCompleted, Is.True);
        });
    }

    private static SkillComboDefinition CreateDefinition(Guid id, out Skill[] skills)
    {
        skills = Enumerable.Range(1, 3)
            .Select(number => new Skill { Id = Guid.NewGuid(), Number = (short)number, Name = $"Skill {number}" })
            .ToArray();
        var definition = new SkillComboDefinition
        {
            Id = id,
            Name = "Test combo",
            MaximumCompletionTime = TimeSpan.FromSeconds(3),
        };
        for (var index = 0; index < skills.Length; index++)
        {
            definition.Steps.Add(new SkillComboStep
            {
                Id = Guid.NewGuid(),
                Order = index + 1,
                IsFinalStep = index == skills.Length - 1,
                Skill = skills[index],
            });
        }

        return definition;
    }
}
