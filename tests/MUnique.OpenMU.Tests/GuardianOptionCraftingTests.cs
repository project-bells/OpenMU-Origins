// <copyright file="GuardianOptionCraftingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// Tests the level 380 Guardian option crafting.
/// </summary>
[TestFixture]
public class GuardianOptionCraftingTests
{
    /// <summary>
    /// Ensures that exactly one eligible item is required by the crafting.
    /// </summary>
    /// <param name="itemLevels">The levels of the eligible input items.</param>
    /// <param name="expectedResult">The expected validation result.</param>
    [TestCase(new byte[] { }, CraftingResult.LackingMixItems)]
    [TestCase(new byte[] { 4 }, null)]
    [TestCase(new byte[] { 4, 7 }, CraftingResult.TooManyItems)]
    public async Task RequiresExactlyOneEligibleItemAsync(byte[] itemLevels, CraftingResult? expectedResult)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await new TalkNpcAction().TalkToNpcAsync(player, CreateCraftingNpc()).ConfigureAwait(false);
        foreach (var level in itemLevels)
        {
            Assert.That(await player.TemporaryStorage!.AddItemAsync(CreateItem(level)).ConfigureAwait(false), Is.True);
        }

        var handler = new GuardianOptionCrafting(CreateSettings());

        var result = handler.TryGetRequiredItems(player, out _, out _);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    private static SimpleCraftingSettings CreateSettings()
    {
        var settings = new Mock<SimpleCraftingSettings>();
        settings.SetupAllProperties();
        settings.Setup(value => value.RequiredItems).Returns(new List<ItemCraftingRequiredItem>
        {
            CreateItemRequirement(4, 6),
            CreateItemRequirement(7, 9),
            CreateItemRequirement(10, 15),
        });
        settings.Setup(value => value.ResultItems).Returns(new List<ItemCraftingResultItem>());
        return settings.Object;
    }

    private static ItemCraftingRequiredItem CreateItemRequirement(byte minimumLevel, byte maximumLevel)
    {
        var requirement = new Mock<ItemCraftingRequiredItem>();
        requirement.SetupAllProperties();
        requirement.Setup(value => value.PossibleItems).Returns(new List<ItemDefinition>());
        requirement.Setup(value => value.RequiredItemOptions).Returns(new List<ItemOptionType>());
        requirement.Object.MinimumAmount = 0;
        requirement.Object.MaximumAmount = 1;
        requirement.Object.MinimumItemLevel = minimumLevel;
        requirement.Object.MaximumItemLevel = maximumLevel;
        requirement.Object.Reference = GuardianOptionCrafting.ItemReference;
        return requirement.Object;
    }

    private static Item CreateItem(byte level)
    {
        var guardianOption = new IncreasableItemOption { OptionType = ItemOptionTypes.GuardianOption };
        var optionDefinition = new Mock<ItemOptionDefinition>();
        optionDefinition.Setup(option => option.PossibleOptions).Returns(new List<IncreasableItemOption> { guardianOption });
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(item => item.PossibleItemOptions).Returns(new List<ItemOptionDefinition> { optionDefinition.Object });
        definition.Object.Width = 1;
        definition.Object.Height = 1;
        definition.Object.Durability = 1;
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(value => value.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Object.Definition = definition.Object;
        item.Object.Durability = 1;
        item.Object.Level = level;
        return item.Object;
    }

    private static NonPlayerCharacter CreateCraftingNpc()
    {
        var definition = new Mock<MonsterDefinition>();
        definition.SetupAllProperties();
        definition.Setup(npc => npc.ItemCraftings).Returns(new List<ItemCrafting> { new() });
        definition.Object.NpcWindow = NpcWindow.ChaosMachine;
        return new NonPlayerCharacter(null!, definition.Object, null!);
    }
}
