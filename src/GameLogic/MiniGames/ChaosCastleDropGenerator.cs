// <copyright file="ChaosCastleDropGenerator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// An <see cref="IDropGenerator"/> especially for Chaos Castle.
/// It will drop a predefined amount of jewels during the whole event,
/// and nothing more.
/// </summary>
public class ChaosCastleDropGenerator : IDropGenerator
{
    private readonly Dictionary<int, DropItemGroup> _monsterDrops = new();
    private readonly IGameContext _gameContext;
    private int _killedMonsters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaosCastleDropGenerator"/> class.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="context">The context.</param>
    /// <param name="spawnedMonstersCount">The spawned monsters count.</param>
    public ChaosCastleDropGenerator(IGameContext gameContext, ChaosCastleContext context, int spawnedMonstersCount)
        : this(
            gameContext,
            context.Definition.Entrance!.Map!.DropItemGroups.Where(group => group.ItemType == SpecialItemType.ChaosCastleJewel),
            spawnedMonstersCount,
            Rand.GetRandomizer())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaosCastleDropGenerator"/> class.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="configuredDrops">The configured drops.</param>
    /// <param name="spawnedMonstersCount">The spawned monsters count.</param>
    /// <param name="randomizer">The randomizer.</param>
    internal ChaosCastleDropGenerator(IGameContext gameContext, IEnumerable<DropItemGroup> configuredDrops, int spawnedMonstersCount, IRandomizer randomizer)
    {
        this._gameContext = gameContext;
        foreach (var configuredDrop in configuredDrops)
        {
            bool assigned;
            do
            {
                var randomKillCount = randomizer.NextInt(1, spawnedMonstersCount + 1);
                assigned = this._monsterDrops.TryAdd(randomKillCount, configuredDrop);
            }
            while (!assigned);
        }
    }

    /// <inheritdoc />
    public async ValueTask<(IEnumerable<Item> Items, uint? Money)> GenerateItemDropsAsync(MonsterDefinition monster, int gainedExperience, Player player)
    {
        var killCount = Interlocked.Increment(ref this._killedMonsters);
        if (this._monsterDrops.TryGetValue(killCount, out var drop)
            && this._gameContext.DropGenerator.GenerateItemDrop(drop) is { } item)
        {
            return (item.GetAsEnumerable(), null);
        }

        return (Enumerable.Empty<Item>(), null);
    }

    /// <inheritdoc />
    public Item? GenerateItemDrop(DropItemGroup group)
    {
        return this._gameContext.DropGenerator.GenerateItemDrop(group);
    }

    /// <inheritdoc />
    public (Item? Item, uint? Money, ItemDropEffect DropEffect) GenerateItemDrop(IEnumerable<DropItemGroup> groups)
    {
        return this._gameContext.DropGenerator.GenerateItemDrop(groups);
    }
}
