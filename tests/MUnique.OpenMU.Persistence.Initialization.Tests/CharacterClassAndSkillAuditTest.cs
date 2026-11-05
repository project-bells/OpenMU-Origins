// <copyright file="CharacterClassAndSkillAuditTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Performs data-driven consistency checks of the Season 6 character classes and skills.
/// </summary>
[TestFixture]
internal class CharacterClassAndSkillAuditTest
{
    private static readonly CharacterClassNumber[][] ClassProgressions =
    [
        [CharacterClassNumber.DarkKnight, CharacterClassNumber.BladeKnight, CharacterClassNumber.BladeMaster],
        [CharacterClassNumber.DarkWizard, CharacterClassNumber.SoulMaster, CharacterClassNumber.GrandMaster],
        [CharacterClassNumber.FairyElf, CharacterClassNumber.MuseElf, CharacterClassNumber.HighElf],
        [CharacterClassNumber.MagicGladiator, CharacterClassNumber.DuelMaster],
        [CharacterClassNumber.DarkLord, CharacterClassNumber.LordEmperor],
        [CharacterClassNumber.Summoner, CharacterClassNumber.BloodySummoner, CharacterClassNumber.DimensionMaster],
        [CharacterClassNumber.RageFighter, CharacterClassNumber.FistMaster],
    ];

    /// <summary>
    /// Verifies the configured progression of every playable character class.
    /// </summary>
    [Test]
    public async Task CharacterClassProgressionsAreCompleteAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            Assert.That(configuration.CharacterClasses, Has.Count.EqualTo(ClassProgressions.Sum(p => p.Length)));
            foreach (var progression in ClassProgressions)
            {
                var classes = progression.Select(number => GetClass(configuration, number)).ToArray();
                Assert.Multiple(() =>
                {
                    Assert.That(classes[0].CanGetCreated, Is.True, classes[0].Name);
                    Assert.That(classes.Skip(1), Has.All.Matches<CharacterClass>(c => !c.CanGetCreated));
                    Assert.That(classes.Take(classes.Length - 1), Has.All.Matches<CharacterClass>(c => !c.IsMasterClass));
                    Assert.That(classes[^1].IsMasterClass, Is.True, classes[^1].Name);
                });

                for (var index = 0; index < classes.Length - 1; index++)
                {
                    Assert.That(classes[index].NextGenerationClass, Is.SameAs(classes[index + 1]), classes[index].Name);
                }

                Assert.That(classes[^1].NextGenerationClass, Is.Null, classes[^1].Name);
                Assert.That(classes[^1].BaseAttributeValues.Any(v => v.Definition == Stats.MasterPointsPerLevelUp), Is.True, classes[^1].Name);
            }
        }
    }

    /// <summary>
    /// Verifies that all configured skills have coherent execution and qualification data.
    /// </summary>
    [Test]
    public async Task SkillsHaveCoherentConfigurationAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            Assert.That(configuration.Skills, Is.Not.Empty);
            Assert.That(configuration.Skills.Select(s => s.Number), Is.Unique);
            foreach (var skill in configuration.Skills)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(skill.NumberOfHitsPerAttack, Is.GreaterThanOrEqualTo(1), skill.Name);
                    Assert.That(skill.Range, Is.GreaterThanOrEqualTo(0), skill.Name);
                    Assert.That(skill.AttackDamage, Is.GreaterThanOrEqualTo(0), skill.Name);
                    Assert.That(skill.QualifiedCharacters, Has.All.Matches<CharacterClass>(configuration.CharacterClasses.Contains));
                    Assert.That(skill.Requirements, Has.All.Matches<AttributeRequirement>(r => r.Attribute is not null));
                    Assert.That(skill.ConsumeRequirements, Has.All.Matches<AttributeRequirement>(r => r.Attribute is not null));
                });
            }
        }
    }

    /// <summary>
    /// Verifies the prerequisite graph and allocation data of every master skill.
    /// </summary>
    [Test]
    public async Task MasterSkillGraphIsCoherentAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            var masterSkills = configuration.Skills.Where(s => s.MasterDefinition is not null).ToList();
            Assert.That(masterSkills, Is.Not.Empty);
            foreach (var skill in masterSkills)
            {
                var definition = skill.MasterDefinition!;
                Assert.Multiple(() =>
                {
                    Assert.That(definition.Root, Is.Not.Null, skill.Name);
                    Assert.That(definition.Rank, Is.InRange(1, 9), skill.Name);
                    Assert.That(definition.MinimumLevel, Is.InRange(1, definition.MaximumLevel), skill.Name);
                    Assert.That(definition.MaximumLevel, Is.GreaterThan(0), skill.Name);
                    Assert.That(definition.ValueFormula, Is.Not.Empty, skill.Name);
                    Assert.That(definition.DisplayValueFormula, Is.Not.Empty, skill.Name);
                    Assert.That(skill.QualifiedCharacters, Has.All.Matches<CharacterClass>(c => c.IsMasterClass), skill.Name);
                });

                foreach (var prerequisite in definition.RequiredMasterSkills)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(prerequisite, Is.Not.SameAs(skill), skill.Name);
                        Assert.That(prerequisite.QualifiedCharacters.Intersect(skill.QualifiedCharacters), Is.Not.Empty, skill.Name);
                    });

                    if (prerequisite.MasterDefinition is { } prerequisiteDefinition)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(prerequisiteDefinition.Root, Is.SameAs(definition.Root), skill.Name);
                            Assert.That(prerequisiteDefinition.Rank, Is.LessThanOrEqualTo(definition.Rank), skill.Name);
                        });
                    }
                }

                if (definition.ReplacedSkill is { } replacedSkill)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(replacedSkill, Is.Not.SameAs(skill), skill.Name);
                        Assert.That(replacedSkill.QualifiedCharacters.Intersect(skill.QualifiedCharacters), Is.Not.Empty, skill.Name);
                    });
                }
            }
        }
    }

    /// <summary>
    /// Verifies that Rage Fighter calculations are expressed through configured total-stat relationships.
    /// </summary>
    [Test]
    public async Task RageFighterFormulasUseConfiguredTotalStatsAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            foreach (var number in new[] { CharacterClassNumber.RageFighter, CharacterClassNumber.FistMaster })
            {
                var characterClass = GetClass(configuration, number);
                var inputs = characterClass.AttributeCombinations
                    .Where(relationship => relationship.TargetAttribute != Stats.TotalStrength
                        && relationship.TargetAttribute != Stats.TotalAgility
                        && relationship.TargetAttribute != Stats.TotalVitality
                        && relationship.TargetAttribute != Stats.TotalEnergy)
                    .Select(relationship => relationship.InputAttribute)
                    .ToList();

                Assert.Multiple(() =>
                {
                    Assert.That(inputs, Does.Contain(Stats.TotalStrength), characterClass.Name);
                    Assert.That(inputs, Does.Contain(Stats.TotalAgility), characterClass.Name);
                    Assert.That(inputs, Does.Contain(Stats.TotalVitality), characterClass.Name);
                    Assert.That(inputs, Does.Contain(Stats.TotalEnergy), characterClass.Name);
                    Assert.That(inputs, Does.Not.Contain(Stats.BaseStrength), characterClass.Name);
                    Assert.That(inputs, Does.Not.Contain(Stats.BaseAgility), characterClass.Name);
                    Assert.That(inputs, Does.Not.Contain(Stats.BaseVitality), characterClass.Name);
                    Assert.That(inputs, Does.Not.Contain(Stats.BaseEnergy), characterClass.Name);
                });
            }
        }
    }

    private static CharacterClass GetClass(GameConfiguration configuration, CharacterClassNumber number)
    {
        return configuration.CharacterClasses.Single(c => c.Number == (byte)number);
    }
}
