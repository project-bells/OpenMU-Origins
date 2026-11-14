// <copyright file="ItemLevelUpgradeCraftingTests.cs" company="MUnique">
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
/// Tests item-level upgrade talisman handling.
/// </summary>
[TestFixture]
public class ItemLevelUpgradeCraftingTests
{
    /// <summary>
    /// Ensures that the two mutually exclusive talismans are rejected together.
    /// </summary>
    [Test]
    public void LuckAndProtectionTalismansAreMutuallyExclusive()
    {
        var crafting = new TestItemLevelUpgradeCrafting();
        var requiredItems = new List<CraftingRequiredItemLink>
        {
            CreateLink(ItemLevelUpgradeCrafting.TalismanOfLuckReference, hasItem: true),
            CreateLink(ItemLevelUpgradeCrafting.TalismanOfChaosAssemblyReference, hasItem: true),
        };

        Assert.That(crafting.Validate(requiredItems), Is.EqualTo(CraftingResult.IncorrectMixItems));
    }

    /// <summary>
    /// Ensures that the protection talisman resets the upgraded item on failure.
    /// </summary>
    [Test]
    public void ProtectionTalismanChangesFailureResultToLevelReset()
    {
        var crafting = new TestItemLevelUpgradeCrafting();
        var upgradeItem = CreateLink(ItemLevelUpgradeCrafting.UpgradeItemReference, hasItem: true, MixResult.Disappear);
        var requiredItems = new List<CraftingRequiredItemLink>
        {
            upgradeItem,
            CreateLink(ItemLevelUpgradeCrafting.TalismanOfChaosAssemblyReference, hasItem: true),
        };

        Assert.That(crafting.GetResult(upgradeItem, requiredItems, success: false), Is.EqualTo(MixResult.ResetItemLevel));
    }

    /// <summary>
    /// Ensures that an unprotected failed upgrade keeps its configured failure result.
    /// </summary>
    [Test]
    public void UpgradeWithoutProtectionKeepsConfiguredFailureResult()
    {
        var crafting = new TestItemLevelUpgradeCrafting();
        var upgradeItem = CreateLink(ItemLevelUpgradeCrafting.UpgradeItemReference, hasItem: true, MixResult.Disappear);
        var requiredItems = new List<CraftingRequiredItemLink> { upgradeItem };

        Assert.That(crafting.GetResult(upgradeItem, requiredItems, success: false), Is.EqualTo(MixResult.Disappear));
    }

    /// <summary>
    /// Ensures that mutually exclusive talismans are rejected even when added via <c>CraftingTalismans</c> with reference <c>0</c>.
    /// </summary>
    [Test]
    public void LuckAndProtectionTalismansAreMutuallyExclusiveByItemDefinition()
    {
        var crafting = new TestItemLevelUpgradeCrafting();
        var requiredItems = new List<CraftingRequiredItemLink>
        {
            CreateTalismanLinkByItemNumber(53),
            CreateTalismanLinkByItemNumber(96),
        };

        Assert.That(crafting.Validate(requiredItems), Is.EqualTo(CraftingResult.IncorrectMixItems));
    }

    /// <summary>
    /// Ensures that protection talismans reset upgraded items even when matched by item definition with reference <c>0</c>.
    /// </summary>
    [Test]
    public void ProtectionTalismanResetsUpgradeItemLevelWhenMatchedByItemDefinition()
    {
        var crafting = new TestItemLevelUpgradeCrafting();
        var upgradeItem = CreateLink(ItemLevelUpgradeCrafting.UpgradeItemReference, hasItem: true, MixResult.Disappear);
        var requiredItems = new List<CraftingRequiredItemLink>
        {
            upgradeItem,
            CreateTalismanLinkByItemNumber(96, MixResult.DisappearAndProtectReferencedItems),
        };

        Assert.That(crafting.GetResult(upgradeItem, requiredItems, success: false), Is.EqualTo(MixResult.ResetItemLevel));
    }

    private static CraftingRequiredItemLink CreateTalismanLinkByItemNumber(short number, MixResult failResult = MixResult.Disappear)
    {
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Object.Group = 14;
        definition.Object.Number = number;

        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Object.Definition = definition.Object;

        var requirement = new Mock<ItemCraftingRequiredItem>();
        requirement.SetupAllProperties();
        requirement.Object.Reference = 0;
        requirement.Object.FailResult = failResult;
        return new CraftingRequiredItemLink([item.Object], requirement.Object);
    }

    private static CraftingRequiredItemLink CreateLink(byte reference, bool hasItem, MixResult failResult = MixResult.StaysAsIs)
    {
        var requirement = new Mock<ItemCraftingRequiredItem>();
        requirement.SetupAllProperties();
        requirement.Object.Reference = reference;
        requirement.Object.FailResult = failResult;
        var items = hasItem ? new[] { new Mock<Item>().Object } : [];
        return new CraftingRequiredItemLink(items, requirement.Object);
    }

    private sealed class TestItemLevelUpgradeCrafting : ItemLevelUpgradeCrafting
    {
        public TestItemLevelUpgradeCrafting()
            : base(new SimpleCraftingSettings())
        {
        }

        public CraftingResult? Validate(IList<CraftingRequiredItemLink> requiredItems)
        {
            return this.ValidateMix(requiredItems, 0);
        }

        public MixResult GetResult(CraftingRequiredItemLink itemLink, IList<CraftingRequiredItemLink> requiredItems, bool success)
        {
            return this.GetMixResult(itemLink, requiredItems, success);
        }
    }
}
