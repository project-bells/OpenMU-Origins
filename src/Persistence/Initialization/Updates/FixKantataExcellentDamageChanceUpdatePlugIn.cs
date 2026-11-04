// <copyright file="FixKantataExcellentDamageChanceUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Corrects the excellent damage chance of the Kantata ancient set from 1000 percent to 10 percent.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.FixKantataExcellentDamageChanceUpdatePlugIn_Name), Description = nameof(PlugInResources.FixKantataExcellentDamageChanceUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("0A8695EC-5D48-4250-AE04-8EC8F0CB9B58")]
public sealed class FixKantataExcellentDamageChanceUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.FixKantataExcellentDamageChanceUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.FixKantataExcellentDamageChanceUpdatePlugIn_Description;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 4, 2, 49, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var excellentDamageChance = Stats.ExcellentDamageChance.GetPersistent(gameConfiguration);
        var kantata = gameConfiguration.ItemSetGroups.FirstOrDefault(set => set.Name.ValueInNeutralLanguage == "Kantata");
        var option = kantata?.Options?.PossibleOptions.FirstOrDefault(candidate =>
            candidate.PowerUpDefinition?.TargetAttribute == excellentDamageChance);
        if (option?.PowerUpDefinition?.Boost is { } boost)
        {
            boost.ConstantValue.Value = 0.10f;
            boost.ConstantValue.AggregateType = AggregateType.AddRaw;
        }

        return ValueTask.CompletedTask;
    }
}
