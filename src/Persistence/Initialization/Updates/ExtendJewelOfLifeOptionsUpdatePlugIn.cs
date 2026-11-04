// <copyright file="ExtendJewelOfLifeOptionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Extends regular Jewel of Life options from level 4 to level 7.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ExtendJewelOfLifeOptionsUpdatePlugIn_Name), Description = nameof(PlugInResources.ExtendJewelOfLifeOptionsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("8DC9870D-ED9B-47A1-B5D8-69B02C89D71B")]
public sealed class ExtendJewelOfLifeOptionsUpdatePlugIn : UpdatePlugInBase
{
    private const int MaximumOptionLevel = 7;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.ExtendJewelOfLifeOptionsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.ExtendJewelOfLifeOptionsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 4, 6, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var optionType = gameConfiguration.ItemOptionTypes.First(type => type == ItemOptionTypes.Option);
        var options = gameConfiguration.ItemOptions
            .SelectMany(definition => definition.PossibleOptions)
            .Where(option => option.OptionType == optionType && option.LevelDependentOptions.Any());
        foreach (var option in options)
        {
            ExtendOption(context, option);
        }

        return ValueTask.CompletedTask;
    }

    private static void ExtendOption(IContext context, IncreasableItemOption option)
    {
        while (option.LevelDependentOptions.Max(level => level.Level) < MaximumOptionLevel)
        {
            var orderedLevels = option.LevelDependentOptions.OrderBy(level => level.Level).ToList();
            if (orderedLevels.Count < 2
                || orderedLevels[^2].PowerUpDefinition?.Boost?.ConstantValue is not { } previousValue
                || orderedLevels[^1].PowerUpDefinition?.Boost?.ConstantValue is not { } currentValue)
            {
                return;
            }

            var powerUp = context.CreateNew<PowerUpDefinition>();
            powerUp.TargetAttribute = orderedLevels[^1].PowerUpDefinition!.TargetAttribute;
            powerUp.Boost = context.CreateNew<PowerUpDefinitionValue>();
            powerUp.Boost.ConstantValue.Value = currentValue.Value + (currentValue.Value - previousValue.Value);
            powerUp.Boost.ConstantValue.AggregateType = currentValue.AggregateType;

            var newLevel = context.CreateNew<ItemOptionOfLevel>();
            newLevel.Level = orderedLevels[^1].Level + 1;
            newLevel.RequiredItemLevel = orderedLevels[^1].RequiredItemLevel;
            newLevel.PowerUpDefinition = powerUp;
            option.LevelDependentOptions.Add(newLevel);
        }
    }
}
