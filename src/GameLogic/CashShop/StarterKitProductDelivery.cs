// <copyright file="StarterKitProductDelivery.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Delivers the seven class-specific, temporary starter kits granted by the web portal.
/// </summary>
internal sealed class StarterKitProductDelivery : ICashShopProductDeliveryPlugIn
{
    internal const int FirstProductSequence = 2001;
    internal const int LastProductSequence = 2007;
    private const byte ItemLevel = 9;
    private const byte OptionLevel = 4;

    private readonly List<Item> _addedItems = [];

    private static readonly IReadOnlyDictionary<int, StarterKitDefinition> Kits =
        new Dictionary<int, StarterKitDefinition>
        {
            [2001] = new(5, 0, 1),
            [2002] = new(2, 5, 0),
            [2003] = new(10, 4, 0),
            [2004] = new(15, 0, 7),
            [2005] = new(25, 2, 8),
            [2006] = new(39, 5, 15),
            [2007] = new(59, 0, 32),
        };

    public ItemIdentifier Key => default;

    public bool CanDeliver(CashShopProduct product) => Kits.ContainsKey(product.ProductSequence);

    public async ValueTask<CashShopUseResult> DeliverAsync(Player player, CashShopProduct product)
    {
        if (player.Inventory is not { } inventory || !Kits.TryGetValue(product.ProductSequence, out var kit))
        {
            return CashShopUseResult.CannotUse;
        }

        var expiresAt = DateTime.UtcNow.Add(product.Duration > TimeSpan.Zero ? product.Duration : TimeSpan.FromDays(7));
        var definitions = player.GameContext.Configuration.Items;
        byte[] armorGroups = [7, 8, 9, 10, 11];
        var kitDefinitions = armorGroups
            .Select(group => definitions.FirstOrDefault(item => item.Group == group && item.Number == kit.ArmorSetNumber))
            .Where(definition => definition is not null)
            .Cast<ItemDefinition>()
            .Append(definitions.First(item => item.Group == kit.WeaponGroup && item.Number == kit.WeaponNumber))
            .Append(definitions.First(item => item.Group == 13 && item.Number == 64)) // Demon pet
            .ToList();

        foreach (var definition in kitDefinitions)
        {
            var item = CreateItem(player.PersistenceContext, definition, expiresAt, definition.Group != 13);
            if (!await inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                foreach (var addedItem in this._addedItems)
                {
                    await inventory.RemoveItemAsync(addedItem).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(addedItem).ConfigureAwait(false);
                }

                this._addedItems.Clear();
                return CashShopUseResult.InventoryFull;
            }

            this._addedItems.Add(item);
        }

        return CashShopUseResult.Success;
    }

    public async ValueTask DeliveredAsync(Player player, CashShopProduct product)
    {
        foreach (var item in this._addedItems)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(view => view.ItemAppearAsync(item)).ConfigureAwait(false);
        }
    }

    private static Item CreateItem(IPlayerContext context, ItemDefinition definition, DateTime expiresAt, bool equipment)
    {
        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        item.Level = equipment ? ItemLevel : (byte)0;
        item.HasSkill = equipment && definition.Group is <= 5;
        item.ExpiresAt = expiresAt;
        item.IsBound = true;
        if (equipment)
        {
            var option = definition.PossibleItemOptions.SelectMany(possible => possible.PossibleOptions)
                .FirstOrDefault(candidate => candidate.OptionType == ItemOptionTypes.Option);
            if (option is not null)
            {
                var optionLink = context.CreateNew<ItemOptionLink>();
                optionLink.ItemOption = option;
                optionLink.Level = OptionLevel;
                item.ItemOptions.Add(optionLink);
            }
        }

        return item;
    }

    private sealed record StarterKitDefinition(byte ArmorSetNumber, byte WeaponGroup, byte WeaponNumber);
}
