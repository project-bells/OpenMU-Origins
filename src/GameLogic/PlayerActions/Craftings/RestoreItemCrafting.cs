// <copyright file="RestoreItemCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Crafting which removes the <see cref="ItemOptionTypes.HarmonyOption"/> from an item.
/// </summary>
public class RestoreItemCrafting : SimpleItemCraftingHandler
{
    private static readonly int[] PricePerOptionLevel = [100_000, 110_000, 120_000, 130_000, 140_000, 150_000, 200_000, 220_000, 240_000, 280_000, 320_000, 360_000, 400_000, 500_000];

    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreItemCrafting"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public RestoreItemCrafting(SimpleCraftingSettings settings)
        : base(settings)
    {
    }

    /// <inheritdoc />
    protected override int GetPrice(byte successRate, IList<CraftingRequiredItemLink> requiredItems)
    {
        var optionLevel = requiredItems.FirstOrDefault()?
            .Items.FirstOrDefault()?
            .ItemOptions.FirstOrDefault(io => io.ItemOption?.OptionType == ItemOptionTypes.HarmonyOption)?
            .Level ?? 0;
        return PricePerOptionLevel[Math.Min(optionLevel, PricePerOptionLevel.Length - 1)];
    }

    /// <inheritdoc />
    protected override async ValueTask<List<Item>> CreateOrModifyResultItemsAsync(IList<CraftingRequiredItemLink> requiredItems, Player player, byte socketSlot, byte successRate)
    {
        var item = requiredItems.First().Items.First();
        var johOptionLink = item.ItemOptions.First(link => link.ItemOption?.OptionType == ItemOptionTypes.HarmonyOption);
        item.ItemOptions.Remove(johOptionLink);
        await player.PersistenceContext.DeleteAsync(johOptionLink).ConfigureAwait(false);
        return new List<Item> { item };
    }
}
