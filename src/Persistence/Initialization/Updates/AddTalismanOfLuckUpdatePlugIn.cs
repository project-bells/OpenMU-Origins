// <copyright file="AddTalismanOfLuckUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the Talisman of Luck to the season 6 item upgrade combinations.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.AddTalismanOfLuckUpdatePlugIn_Name), Description = nameof(PlugInResources.AddTalismanOfLuckUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("5B7063D7-E30D-4B40-8499-5C3F1D567B61")]
public sealed class AddTalismanOfLuckUpdatePlugIn : UpdatePlugInBase
{
    private static readonly int[] ItemUpgradeCraftingNumbers = [3, 4, 22, 23, 49, 50];

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.AddTalismanOfLuckUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.AddTalismanOfLuckUpdatePlugIn_Description;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 4, 7, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var talisman = gameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == 53)
                       ?? CreateTalisman(context, gameConfiguration);
        var craftings = gameConfiguration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings;
        foreach (var craftingNumber in ItemUpgradeCraftingNumbers)
        {
            var settings = craftings.Single(crafting => crafting.Number == craftingNumber).SimpleCraftingSettings!;
            if (settings.RequiredItems.Any(requirement => requirement.PossibleItems.Contains(talisman)))
            {
                continue;
            }

            var requirement = context.CreateNew<ItemCraftingRequiredItem>();
            requirement.MinimumAmount = 0;
            requirement.MaximumAmount = 1;
            requirement.AddPercentage = 10;
            requirement.SuccessResult = MixResult.Disappear;
            requirement.FailResult = MixResult.Disappear;
            requirement.PossibleItems.Add(talisman);
            settings.RequiredItems.Add(requirement);
        }

        if (gameConfiguration.CashShopConfiguration is { } cashShop)
        {
            foreach (var package in cashShop.Packages.Where(package => package.PackageSequence is 98 or 99 or 389))
            {
                foreach (var product in package.Products)
                {
                    product.ItemDefinition = talisman;
                }

                package.IsForSale = true;
            }
        }

        return ValueTask.CompletedTask;
    }

    private static ItemDefinition CreateTalisman(IContext context, GameConfiguration gameConfiguration)
    {
        var talisman = context.CreateNew<ItemDefinition>();
        talisman.Name = LocalizedString.FromResource(() => ItemNames.TalismanOfLuck);
        talisman.Number = 53;
        talisman.Group = 14;
        talisman.DropsFromMonsters = false;
        talisman.Durability = 1;
        talisman.Width = 1;
        talisman.Height = 1;
        talisman.SetGuid(talisman.Group, talisman.Number);
        gameConfiguration.Items.Add(talisman);
        return talisman;
    }
}
