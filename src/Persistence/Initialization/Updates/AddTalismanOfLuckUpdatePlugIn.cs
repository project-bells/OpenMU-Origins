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
using ItemLevelUpgradeHandler = MUnique.OpenMU.GameLogic.PlayerActions.Craftings.ItemLevelUpgradeCrafting;

/// <summary>
/// Adds the talismans supported by the season 6 item upgrade combinations.
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
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 4, 7, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var talisman = gameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == 53)
                       ?? CreateTalisman(context, gameConfiguration);
        talisman.Durability = 10;
        var protectionTalisman = gameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == 96)
                                 ?? CreateProtectionTalisman(context, gameConfiguration);
        var craftings = gameConfiguration.Monsters.Single(monster => monster.NpcWindow == NpcWindow.ChaosMachine).ItemCraftings;
        foreach (var craftingNumber in ItemUpgradeCraftingNumbers)
        {
            var crafting = craftings.Single(crafting => crafting.Number == craftingNumber);
            crafting.ItemCraftingHandlerClassName = typeof(ItemLevelUpgradeHandler).FullName!;
            var settings = crafting.SimpleCraftingSettings!;
            var upgradeItem = settings.RequiredItems.Single(requirement => requirement.Reference == ItemLevelUpgradeHandler.UpgradeItemReference);
            upgradeItem.Reference = ItemLevelUpgradeHandler.UpgradeItemReference;

            var luckRequirement = settings.RequiredItems.FirstOrDefault(requirement => requirement.PossibleItems.Contains(talisman));
            if (luckRequirement is null)
            {
                luckRequirement = context.CreateNew<ItemCraftingRequiredItem>();
                luckRequirement.PossibleItems.Add(talisman);
                settings.RequiredItems.Add(luckRequirement);
            }

            luckRequirement.MinimumAmount = 0;
            luckRequirement.MaximumAmount = 10;
            luckRequirement.AddPercentage = 1;
            luckRequirement.SuccessResult = MixResult.Disappear;
            luckRequirement.FailResult = MixResult.Disappear;
            luckRequirement.Reference = ItemLevelUpgradeHandler.TalismanOfLuckReference;

            var protectionRequirement = settings.RequiredItems.FirstOrDefault(requirement => requirement.PossibleItems.Contains(protectionTalisman));
            if (protectionRequirement is null)
            {
                protectionRequirement = context.CreateNew<ItemCraftingRequiredItem>();
                protectionRequirement.PossibleItems.Add(protectionTalisman);
                settings.RequiredItems.Add(protectionRequirement);
            }

            protectionRequirement.MinimumAmount = 0;
            protectionRequirement.MaximumAmount = 1;
            protectionRequirement.SuccessResult = MixResult.Disappear;
            protectionRequirement.FailResult = MixResult.DisappearAndProtectReferencedItems;
            protectionRequirement.Reference = ItemLevelUpgradeHandler.TalismanOfChaosAssemblyReference;
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
        talisman.Durability = 10;
        talisman.Width = 1;
        talisman.Height = 1;
        talisman.SetGuid(talisman.Group, talisman.Number);
        gameConfiguration.Items.Add(talisman);
        return talisman;
    }

    private static ItemDefinition CreateProtectionTalisman(IContext context, GameConfiguration gameConfiguration)
    {
        LocalizedStringResources.Register(nameof(ItemNames), ItemNames.ResourceManager);
        var talisman = context.CreateNew<ItemDefinition>();
        talisman.Name = ItemNames.ResourceManager.GetLocalizedString("TalismanOfChaosAssembly");
        talisman.Number = 96;
        talisman.Group = 14;
        talisman.DropsFromMonsters = false;
        talisman.Durability = 10;
        talisman.Width = 1;
        talisman.Height = 1;
        talisman.SetGuid(talisman.Group, talisman.Number);
        gameConfiguration.Items.Add(talisman);
        return talisman;
    }
}
