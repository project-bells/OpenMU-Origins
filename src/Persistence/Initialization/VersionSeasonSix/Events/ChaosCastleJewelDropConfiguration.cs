// <copyright file="ChaosCastleJewelDropConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Creates the configured jewel drops which are distributed across Chaos Castle monster kills.
/// </summary>
internal static class ChaosCastleJewelDropConfiguration
{
    private static readonly (byte Blesses, byte Souls)[] DropsPerLevel =
    {
        (0, 0),
        (0, 2),
        (1, 1),
        (1, 2),
        (1, 2),
        (2, 1),
        (2, 2),
        (2, 3),
    };

    /// <summary>
    /// Ensures that the configured jewel drops exist and are attached to the event map.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="definition">The Chaos Castle definition.</param>
    internal static void Ensure(IContext context, GameConfiguration gameConfiguration, MiniGameDefinition definition)
    {
        var map = definition.Entrance!.Map!;
        var drops = DropsPerLevel[definition.GameLevel];
        var items = Enumerable.Repeat((Group: (byte)14, Number: (short)13), drops.Blesses)
            .Concat(Enumerable.Repeat((Group: (byte)14, Number: (short)14), drops.Souls))
            .ToList();
        for (var index = 0; index < items.Count; index++)
        {
            var groupId = GuidHelper.CreateGuid<DropItemGroup>(map.Number, 3, (byte)index);
            var group = gameConfiguration.DropItemGroups.FirstOrDefault(candidate => candidate.GetId() == groupId);
            if (group is null)
            {
                group = context.CreateNew<DropItemGroup>();
                group.SetGuid(map.Number, 3, (byte)index);
                gameConfiguration.DropItemGroups.Add(group);
            }

            group.Description = DropGroupDescriptions.ResourceManager.GetLocalizedString($"RewardedJewelsForChaosCastle{definition.GameLevel}");
            group.Chance = 1;
            group.ItemType = SpecialItemType.ChaosCastleJewel;
            var item = gameConfiguration.Items.Single(candidate => candidate.Group == items[index].Group && candidate.Number == items[index].Number);
            if (group.PossibleItems.Count != 1 || group.PossibleItems.Single() != item)
            {
                group.PossibleItems.Clear();
                group.PossibleItems.Add(item);
            }

            if (!map.DropItemGroups.Contains(group))
            {
                map.DropItemGroups.Add(group);
            }
        }
    }
}
