// <copyright file="ItemLevelUpgradeCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// Handles item upgrades from level 9 through level 15, including protection by a Talisman of Chaos Assembly.
/// </summary>
public class ItemLevelUpgradeCrafting : SimpleItemCraftingHandler
{
    /// <summary>
    /// The reference of the item which is upgraded.
    /// </summary>
    public const byte UpgradeItemReference = 1;

    /// <summary>
    /// The reference of the Talisman of Luck requirement.
    /// </summary>
    public const byte TalismanOfLuckReference = 2;

    /// <summary>
    /// The reference of the Talisman of Chaos Assembly requirement.
    /// </summary>
    public const byte TalismanOfChaosAssemblyReference = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemLevelUpgradeCrafting"/> class.
    /// </summary>
    /// <param name="settings">The crafting settings.</param>
    public ItemLevelUpgradeCrafting(SimpleCraftingSettings settings)
        : base(settings)
    {
    }

    /// <inheritdoc />
    protected override CraftingResult? ValidateMix(IList<CraftingRequiredItemLink> requiredItems, byte socketSlot)
    {
        var usesLuck = HasItems(requiredItems, TalismanOfLuckReference);
        var usesProtection = HasItems(requiredItems, TalismanOfChaosAssemblyReference);
        return usesLuck && usesProtection ? CraftingResult.IncorrectMixItems : null;
    }

    /// <inheritdoc />
    protected override MixResult GetMixResult(CraftingRequiredItemLink itemLink, IList<CraftingRequiredItemLink> requiredItems, bool success)
    {
        if (!success
            && itemLink.ItemRequirement.Reference == UpgradeItemReference
            && HasItems(requiredItems, TalismanOfChaosAssemblyReference))
        {
            return MixResult.ResetItemLevel;
        }

        return base.GetMixResult(itemLink, requiredItems, success);
    }

    private static bool HasItems(IList<CraftingRequiredItemLink> requiredItems, byte reference)
    {
        return requiredItems.Any(link => link.ItemRequirement.Reference == reference && link.Items.Any());
    }
}
