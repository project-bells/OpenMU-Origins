// <copyright file="SimpleItemCraftingHandlerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Tests the <see cref="SimpleItemCraftingHandler"/>.
/// </summary>
[TestFixture]
public class SimpleItemCraftingHandlerTests
{
    /// <summary>
    /// Ensures that crafted excellent options respect the configured crafting limit.
    /// </summary>
    [Test]
    public async Task CraftedExcellentOptionsRespectConfiguredMaximumAsync()
    {
        var settings = new Mock<SimpleCraftingSettings>();
        settings.SetupAllProperties();
        settings.Object.ResultItemExcellentOptionChance = 100;
        settings.Object.ResultItemMaxExcOptionCount = 1;

        var excellentOptions = new Mock<ItemOptionDefinition>();
        excellentOptions.SetupAllProperties();
        excellentOptions.Object.MaximumOptionsPerItem = 6;
        excellentOptions.Setup(option => option.PossibleOptions).Returns(new List<IncreasableItemOption>
        {
            new() { OptionType = ItemOptionTypes.Excellent, Number = 1 },
            new() { OptionType = ItemOptionTypes.Excellent, Number = 2 },
        });
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(item => item.PossibleItemOptions).Returns(new List<ItemOptionDefinition> { excellentOptions.Object });
        var resultItem = new Mock<Item>();
        resultItem.SetupAllProperties();
        resultItem.Setup(item => item.ItemOptions).Returns(new List<ItemOptionLink>());
        resultItem.Object.Definition = definition.Object;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);

        new TestSimpleItemCraftingHandler(settings.Object).AddExcellentOptions(resultItem.Object, player);

        Assert.That(resultItem.Object.ItemOptions, Has.Count.EqualTo(1));
        Assert.That(resultItem.Object.ItemOptions.Single().ItemOption, Is.Not.Null);
    }

    /// <summary>
    /// Ensures that a negative socket-item adjustment is clamped at zero instead of wrapping around.
    /// </summary>
    [Test]
    public async Task NegativeSocketItemSuccessAdjustmentDoesNotUnderflowAsync()
    {
        var requirement = new Mock<ItemCraftingRequiredItem>();
        requirement.SetupAllProperties();
        requirement.Setup(item => item.PossibleItems).Returns(new List<ItemDefinition>());
        requirement.Setup(item => item.RequiredItemOptions).Returns(new List<ItemOptionType>());
        requirement.Object.MinimumAmount = 1;
        requirement.Object.MaximumAmount = 1;
        requirement.Object.MaximumItemLevel = 15;

        var settings = new Mock<SimpleCraftingSettings>();
        settings.SetupAllProperties();
        settings.Setup(item => item.RequiredItems).Returns(new List<ItemCraftingRequiredItem> { requirement.Object });
        settings.Setup(item => item.ResultItems).Returns(new List<ItemCraftingResultItem>());
        settings.Object.SuccessPercent = 5;
        settings.Object.SuccessPercentageAdditionForSocketItem = -20;

        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await new TalkNpcAction().TalkToNpcAsync(player, CreateCraftingNpc()).ConfigureAwait(false);
        var socketItem = CreateSocketItem();
        Assert.That(await player.TemporaryStorage!.AddItemAsync(0, socketItem).ConfigureAwait(false), Is.True);

        var handler = new SimpleItemCraftingHandler(settings.Object);
        var result = handler.TryGetRequiredItems(player, out _, out var successRate);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(successRate, Is.Zero);
        });
    }

    private static NonPlayerCharacter CreateCraftingNpc()
    {
        var definition = new Mock<MonsterDefinition>();
        definition.SetupAllProperties();
        definition.Setup(npc => npc.ItemCraftings).Returns(new List<ItemCrafting> { new() });
        definition.Object.NpcWindow = NpcWindow.ChaosMachine;
        return new NonPlayerCharacter(null!, definition.Object, null!);
    }

    private static Item CreateSocketItem()
    {
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(value => value.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(value => value.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = new ItemDefinition
        {
            Width = 1,
            Height = 1,
            Durability = 1,
        };
        item.Object.Durability = 1;
        item.Object.SocketCount = 1;
        return item.Object;
    }

    private sealed class TestSimpleItemCraftingHandler : SimpleItemCraftingHandler
    {
        public TestSimpleItemCraftingHandler(SimpleCraftingSettings settings)
            : base(settings)
        {
        }

        public void AddExcellentOptions(Item resultItem, Player player)
        {
            this.AddRandomExcellentOptions(resultItem, player);
        }
    }
}
