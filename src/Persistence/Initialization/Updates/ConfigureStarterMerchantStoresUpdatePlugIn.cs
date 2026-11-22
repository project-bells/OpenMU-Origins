// <copyright file="ConfigureStarterMerchantStoresUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Configures compact potion stores and class-specific starter equipment merchants.
/// </summary>
[PlugIn]
[Display(Name = "Configure starter merchant stores", Description = "Adds +7+16 starter sets for every class and replaces small potion stacks with 255-unit stacks.")]
[Guid("763691B5-E756-46E9-BC3D-BACFCE72720D")]
public sealed class ConfigureStarterMerchantStoresUpdatePlugIn : UpdatePlugInBase
{
    private const byte StarterItemLevel = 7;
    private const byte StarterOptionLevel = 4; // +16 option

    /// <inheritdoc />
    public override string DataInitializationKey => DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Configure starter merchant stores";

    /// <inheritdoc />
    public override string Description => "Adds class-specific +7+16 starter equipment and compact 255-unit potion and ammunition stacks.";

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override bool RunsOnNewDatabase => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 22, 13, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var itemHelper = new ItemHelper(context, gameConfiguration);
        ConfigureConsumableStores(gameConfiguration, itemHelper);
        RemoveQuestItemsFromNpcStores(gameConfiguration);

        // One focused starter merchant per class. This stays within the stock client's 8x15 shop grid.
        ConfigureStarterStore(gameConfiguration, itemHelper, 248, 5, ItemGroups.Swords, 1); // Dark Knight: Leather + Short Sword
        ConfigureStarterStore(gameConfiguration, itemHelper, 250, 15, ItemGroups.Swords, 7); // Magic Gladiator: Storm Crow + Falchion
        ConfigureStarterStore(gameConfiguration, itemHelper, 242, 10, ItemGroups.Bows, 0); // Fairy Elf: Vine + Short Bow
        ConfigureStarterStore(gameConfiguration, itemHelper, 245, 2, ItemGroups.Staff, 0); // Dark Wizard: Pad + Skull Staff
        ConfigureStarterStore(gameConfiguration, itemHelper, 416, 39, ItemGroups.Staff, 15); // Summoner: Violent Wind + Violent Wind Stick
        ConfigureStarterStore(gameConfiguration, itemHelper, 251, 25, ItemGroups.Scepters, 8); // Dark Lord: Light Plate + Battle Scepter
        ConfigureStarterStore(gameConfiguration, itemHelper, 578, 59, ItemGroups.Swords, 32); // Rage Fighter: Sacred Fire + Sacred Fire Glove

        return ValueTask.CompletedTask;
    }

    private static void RemoveQuestItemsFromNpcStores(GameConfiguration gameConfiguration)
    {
        var questItems = gameConfiguration.Monsters
            .SelectMany(monster => monster.Quests)
            .SelectMany(quest => quest.RequiredItems)
            .Where(requirement => requirement.DropItemGroup is not null)
            .SelectMany(requirement => requirement.DropItemGroup!.PossibleItems)
            .ToHashSet();
        foreach (var store in gameConfiguration.Monsters
                     .Where(monster => monster.MerchantStore is not null)
                     .Select(monster => monster.MerchantStore!)
                     .Distinct())
        {
            foreach (var item in store.Items.Where(item => item.Definition is not null && questItems.Contains(item.Definition)).ToList())
            {
                store.Items.Remove(item);
            }
        }
    }

    private static void ConfigureConsumableStores(GameConfiguration gameConfiguration, ItemHelper itemHelper)
    {
        foreach (var store in gameConfiguration.Monsters
                     .Where(monster => monster.MerchantStore?.Items.Any(IsPotionOrAmmunition) is true)
                     .Select(monster => monster.MerchantStore!)
                     .Distinct())
        {
            foreach (var item in store.Items.Where(IsPotionOrAmmunition).ToList())
            {
                store.Items.Remove(item);
            }

            store.Items.Add(itemHelper.CreatePotion(0, 3, byte.MaxValue, 0)); // Large Healing Potion x255
            store.Items.Add(itemHelper.CreatePotion(1, 6, byte.MaxValue, 0)); // Large Mana Potion x255
            store.Items.Add(itemHelper.CreatePotion(2, 8, byte.MaxValue, 0)); // Antidote x255
            store.Items.Add(itemHelper.CreateItem(3, 7, (byte)ItemGroups.Bows, byte.MaxValue, 0)); // Bolt x255
            store.Items.Add(itemHelper.CreateItem(4, 15, (byte)ItemGroups.Bows, byte.MaxValue, 0)); // Arrow x255
        }
    }

    private static bool IsPotionOrAmmunition(Item item)
        => item.Definition is { } definition
           && ((definition.Group == 14 && definition.Number is >= 0 and <= 8)
               || (definition.Group == (byte)ItemGroups.Bows && definition.Number is 7 or 15));

    private static void ConfigureStarterStore(
        GameConfiguration gameConfiguration,
        ItemHelper itemHelper,
        short merchantNumber,
        byte armorSetNumber,
        ItemGroups weaponGroup,
        byte weaponNumber)
    {
        var store = gameConfiguration.Monsters.Single(monster => monster.Number == merchantNumber).MerchantStore
                    ?? throw new InvalidOperationException($"Merchant {merchantNumber} has no store.");
        store.Items.Clear();

        ItemGroups[] armorGroups = [ItemGroups.Helm, ItemGroups.Armor, ItemGroups.Pants, ItemGroups.Gloves, ItemGroups.Boots];
        var items = armorGroups
            .Where(group => gameConfiguration.Items.Any(item => item.Group == (byte)group && item.Number == armorSetNumber))
            .Select(group => itemHelper.CreateSetItem(0, armorSetNumber, group, null, StarterItemLevel, StarterOptionLevel, false))
            .ToList();
        items.Add(itemHelper.CreateWeapon(0, weaponGroup, weaponNumber, StarterItemLevel, StarterOptionLevel, false, true, null));

        PackItems(items);
        foreach (var item in items)
        {
            store.Items.Add(item);
        }
    }

    private static void PackItems(IEnumerable<Item> items)
    {
        const int width = 8;
        const int height = 15;
        var occupied = new bool[width, height];

        foreach (var item in items)
        {
            var itemWidth = item.Definition?.Width ?? 1;
            var itemHeight = item.Definition?.Height ?? 1;
            bool placed = false;
            for (int y = 0; y <= height - itemHeight && !placed; y++)
            {
                for (int x = 0; x <= width - itemWidth && !placed; x++)
                {
                    if (!CanPlace(occupied, x, y, itemWidth, itemHeight))
                    {
                        continue;
                    }

                    item.ItemSlot = checked((byte)((y * width) + x));
                    MarkOccupied(occupied, x, y, itemWidth, itemHeight);
                    placed = true;
                }
            }

            if (!placed)
            {
                throw new InvalidOperationException("Starter equipment does not fit into the merchant shop grid.");
            }
        }
    }

    private static bool CanPlace(bool[,] occupied, int x, int y, int width, int height)
    {
        for (int offsetY = 0; offsetY < height; offsetY++)
        {
            for (int offsetX = 0; offsetX < width; offsetX++)
            {
                if (occupied[x + offsetX, y + offsetY])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void MarkOccupied(bool[,] occupied, int x, int y, int width, int height)
    {
        for (int offsetY = 0; offsetY < height; offsetY++)
        {
            for (int offsetX = 0; offsetX < width; offsetX++)
            {
                occupied[x + offsetX, y + offsetY] = true;
            }
        }
    }
}
