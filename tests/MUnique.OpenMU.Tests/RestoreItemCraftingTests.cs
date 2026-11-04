// <copyright file="RestoreItemCraftingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Tests the Jewel of Harmony option restoration crafting.
/// </summary>
[TestFixture]
public class RestoreItemCraftingTests
{
    /// <summary>
    /// Ensures that the highest Harmony option level uses the maximum configured price without an index overflow.
    /// </summary>
    [Test]
    public void HighestHarmonyOptionLevelUsesMaximumPrice()
    {
        var option = new IncreasableItemOption { OptionType = ItemOptionTypes.HarmonyOption };
        var item = new Mock<Item>();
        item.Setup(value => value.ItemOptions).Returns(new List<ItemOptionLink>
        {
            new() { ItemOption = option, Level = 14 },
        });
        var requiredItems = new List<CraftingRequiredItemLink>
        {
            new([item.Object], new ItemCraftingRequiredItem()),
        };
        var crafting = new TestRestoreItemCrafting(new SimpleCraftingSettings());

        var price = crafting.GetCraftingPrice(requiredItems);

        Assert.That(price, Is.EqualTo(500_000));
    }

    private sealed class TestRestoreItemCrafting : RestoreItemCrafting
    {
        public TestRestoreItemCrafting(SimpleCraftingSettings settings)
            : base(settings)
        {
        }

        public int GetCraftingPrice(IList<CraftingRequiredItemLink> requiredItems)
        {
            return this.GetPrice(100, requiredItems);
        }
    }
}
