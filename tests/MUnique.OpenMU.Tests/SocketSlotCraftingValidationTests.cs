// <copyright file="SocketSlotCraftingValidationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// Tests validation of the selected socket slot before a crafting starts.
/// </summary>
[TestFixture]
public class SocketSlotCraftingValidationTests
{
    /// <summary>
    /// Ensures that mounting rejects a slot which does not exist or is already occupied.
    /// </summary>
    /// <param name="socketSlot">The selected socket slot.</param>
    /// <param name="occupiedSlot">The occupied socket slot, or <see langword="null"/> when all sockets are empty.</param>
    /// <param name="expectedResult">The expected validation result.</param>
    [TestCase(3, null, CraftingResult.IncorrectMixItems)]
    [TestCase(1, 1, CraftingResult.IncorrectMixItems)]
    [TestCase(1, null, null)]
    public void MountValidatesSelectedSocket(byte socketSlot, byte? occupiedSlot, CraftingResult? expectedResult)
    {
        var socketItem = CreateSocketItem(3, occupiedSlot);
        var crafting = new TestMountSeedSphereCrafting(new SimpleCraftingSettings());

        var result = crafting.Validate(CreateRequiredItems(socketItem, MountSeedSphereCrafting.SocketItemReference), socketSlot);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    /// <summary>
    /// Ensures that removing rejects an empty or non-existing socket slot.
    /// </summary>
    /// <param name="socketSlot">The selected socket slot.</param>
    /// <param name="occupiedSlot">The occupied socket slot.</param>
    /// <param name="expectedResult">The expected validation result.</param>
    [TestCase(2, 1, CraftingResult.IncorrectMixItems)]
    [TestCase(3, 1, CraftingResult.IncorrectMixItems)]
    [TestCase(1, 1, null)]
    public void RemovalValidatesSelectedSocket(byte socketSlot, byte occupiedSlot, CraftingResult? expectedResult)
    {
        var socketItem = CreateSocketItem(3, occupiedSlot);
        var crafting = new TestRemoveSeedSphereCrafting(new SimpleCraftingSettings());

        var result = crafting.Validate(CreateRequiredItems(socketItem, RemoveSeedSphereCrafting.SocketItemReference), socketSlot);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    private static Item CreateSocketItem(byte socketCount, byte? occupiedSlot)
    {
        var options = new List<ItemOptionLink>();
        if (occupiedSlot is { } index)
        {
            options.Add(new ItemOptionLink
            {
                Index = index,
                ItemOption = new IncreasableItemOption { OptionType = ItemOptionTypes.SocketOption },
            });
        }

        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(value => value.ItemOptions).Returns(options);
        item.Object.SocketCount = socketCount;
        return item.Object;
    }

    private static IList<CraftingRequiredItemLink> CreateRequiredItems(Item socketItem, byte reference)
    {
        var requirement = new Mock<ItemCraftingRequiredItem>();
        requirement.SetupAllProperties();
        requirement.Object.Reference = reference;
        return new List<CraftingRequiredItemLink> { new([socketItem], requirement.Object) };
    }

    private sealed class TestMountSeedSphereCrafting : MountSeedSphereCrafting
    {
        public TestMountSeedSphereCrafting(SimpleCraftingSettings settings)
            : base(settings)
        {
        }

        public CraftingResult? Validate(IList<CraftingRequiredItemLink> requiredItems, byte socketSlot)
        {
            return this.ValidateMix(requiredItems, socketSlot);
        }
    }

    private sealed class TestRemoveSeedSphereCrafting : RemoveSeedSphereCrafting
    {
        public TestRemoveSeedSphereCrafting(SimpleCraftingSettings settings)
            : base(settings)
        {
        }

        public CraftingResult? Validate(IList<CraftingRequiredItemLink> requiredItems, byte socketSlot)
        {
            return this.ValidateMix(requiredItems, socketSlot);
        }
    }
}
