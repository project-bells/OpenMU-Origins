// <copyright file="RepairKnightComboAndFenrirSkillUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Repairs the Blade Knight combo graph and the intrinsic Plasma Storm skill of the Fenrir
/// in databases which were initialized before these links were complete.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("78E1C6C7-B9C0-45E6-8CB6-236E8B91F48D")]
public sealed class RepairKnightComboAndFenrirSkillUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Repair Knight combo and Fenrir skill";
    private const string PlugInDescription = "Restores the three-step Knight combo and makes Plasma Storm available on every Horn of Fenrir.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var bladeKnight = gameConfiguration.CharacterClasses.Single(c => c.Number == (byte)CharacterClassNumber.BladeKnight);
        var combo = bladeKnight.ComboDefinition;
        if (combo is null)
        {
            combo = context.CreateNew<SkillComboDefinition>();
            combo.SetGuid(new Guid("39C94B27-4A87-4A9B-B5A0-C83208657451"));
            combo.Name = LocalizedString.FromResource(() => SkillComboNames.BladeKnightCombo);
            bladeKnight.ComboDefinition = combo;
        }

        combo.MaximumCompletionTime = TimeSpan.FromSeconds(3);
        combo.Steps.Clear();

        AddSteps(context, gameConfiguration, combo, 1, false,
            SkillNumber.Slash, SkillNumber.Cyclone, SkillNumber.Lunge, SkillNumber.FallingSlash, SkillNumber.Uppercut);
        AddSteps(context, gameConfiguration, combo, 2, false,
            SkillNumber.TwistingSlash, SkillNumber.RagefulBlow, SkillNumber.DeathStab, SkillNumber.StrikeofDestruction);
        AddSteps(context, gameConfiguration, combo, 3, true,
            SkillNumber.TwistingSlash, SkillNumber.RagefulBlow, SkillNumber.DeathStab);

        var plasmaStorm = gameConfiguration.Skills.Single(skill => skill.Number == (short)SkillNumber.PlasmaStorm);
        var fenrir = gameConfiguration.Items.Single(item => item is { Group: 13, Number: 37 });
        fenrir.Skill = plasmaStorm;

        foreach (var characterClass in gameConfiguration.DetermineCharacterClasses(CharacterClasses.AllMastersAndSecondClass))
        {
            if (!plasmaStorm.QualifiedCharacters.Contains(characterClass))
            {
                plasmaStorm.QualifiedCharacters.Add(characterClass);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static void AddSteps(
        IContext context,
        GameConfiguration gameConfiguration,
        SkillComboDefinition combo,
        int order,
        bool isFinal,
        params SkillNumber[] skillNumbers)
    {
        foreach (var skillNumber in skillNumbers)
        {
            var step = context.CreateNew<SkillComboStep>();
            step.Skill = gameConfiguration.Skills.Single(skill => skill.Number == (short)skillNumber);
            step.Order = order;
            step.IsFinalStep = isFinal;
            combo.Steps.Add(step);
        }
    }
}
