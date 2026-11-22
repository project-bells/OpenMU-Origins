// <copyright file="StarterMerchantStoresUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.Items;

/// <summary>
/// Tests the compact consumable stores and starter equipment merchants.
/// </summary>
[TestFixture]
internal class StarterMerchantStoresUpdateTest
{
    private static readonly short[] StarterMerchantNumbers = [242, 245, 248, 250, 251, 416, 578];

    /// <summary>
    /// Every class-specific starter shop contains a complete +7+16 set and weapon.
    /// </summary>
    [Test]
    public async Task StarterStoresContainCompleteUpgradedEquipmentAsync()
    {
        var (context, gameConfiguration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;

        foreach (var merchantNumber in StarterMerchantNumbers)
        {
            var items = gameConfiguration.Monsters.Single(monster => monster.Number == merchantNumber).MerchantStore!.Items;
            Assert.That(items, Has.Count.GreaterThanOrEqualTo(5), $"merchant {merchantNumber}");
            Assert.That(items.Select(item => item.Level), Is.All.EqualTo(7), $"merchant {merchantNumber} level");
            Assert.That(items, Has.All.Matches<MUnique.OpenMU.DataModel.Entities.Item>(item =>
                item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Option && link.Level == 4)),
                $"merchant {merchantNumber} option");
        }
    }

    /// <summary>
    /// General stores offer one maximum-size stack of each supported consumable.
    /// </summary>
    [Test]
    public async Task ConsumableStoresUseOnlyMaximumSizeStacksAsync()
    {
        var (context, gameConfiguration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;

        var store = gameConfiguration.Monsters.Single(monster => monster.Number == 253).MerchantStore!;
        var consumables = store.Items
            .Where(item => item.Definition is { } definition
                           && ((definition.Group == 14 && definition.Number is 3 or 6 or 8)
                               || (definition.Group == (byte)ItemGroups.Bows && definition.Number is 7 or 15)))
            .ToList();
        Assert.That(consumables, Has.Count.EqualTo(5));
        Assert.That(consumables.Select(item => item.Durability), Is.All.EqualTo(255));
        Assert.That(store.Items.Any(item => item.Definition is { Group: 14, Number: >= 0 and <= 8 }
                                                 && item.Definition.Number is not (3 or 6 or 8)), Is.False);
    }

    /// <summary>
    /// Class quest drops aren't sold by regular NPC merchants.
    /// </summary>
    [Test]
    public async Task NpcStoresDoNotSellClassQuestItemsAsync()
    {
        var (context, gameConfiguration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;
        var questItems = gameConfiguration.Monsters
            .SelectMany(monster => monster.Quests)
            .SelectMany(quest => quest.RequiredItems)
            .Where(requirement => requirement.DropItemGroup is not null)
            .SelectMany(requirement => requirement.DropItemGroup!.PossibleItems)
            .ToHashSet();

        var soldQuestItems = gameConfiguration.Monsters
            .Where(monster => monster.MerchantStore is not null)
            .SelectMany(monster => monster.MerchantStore!.Items)
            .Where(item => item.Definition is not null && questItems.Contains(item.Definition))
            .ToList();
        Assert.That(soldQuestItems, Is.Empty);
    }
}
