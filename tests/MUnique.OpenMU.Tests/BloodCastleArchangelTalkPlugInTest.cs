// <copyright file="BloodCastleArchangelTalkPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Reflection;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Character;

/// <summary>
/// Tests for the <see cref="BloodCastleArchangelTalkPlugIn"/>.
/// </summary>
[TestFixture]
public class BloodCastleArchangelTalkPlugInTest
{
    private const short OtherNpcNumber = 233;

    private readonly List<IAsyncDisposable> _disposables = [];

    /// <summary>
    /// Disposes the created mini game contexts.
    /// </summary>
    [TearDown]
    public async ValueTask TearDownAsync()
    {
        foreach (var disposable in this._disposables)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

        this._disposables.Clear();
    }

    /// <summary>
    /// Tests that talking to the Archangel inside the blood castle is handled by the plugin.
    /// </summary>
    [Test]
    public async ValueTask TalkingToArchangelInBloodCastleIsHandledAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.CurrentMiniGame = this.CreateBloodCastle(player);
        var eventArgs = new NpcTalkEventArgs();

        await CreatePlugIn()
            .PlayerTalksToNpcAsync(player, CreateNpc(player, BloodCastleArchangelTalkPlugIn.ArchangelNumber), eventArgs)
            .ConfigureAwait(false);

        Assert.That(eventArgs.HasBeenHandled, Is.True);
        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowDialogPlugIn>()!)
            .Verify(p => p.ShowDialogAsync(It.IsAny<byte>(), It.IsAny<byte>()), Times.Once);
    }

    /// <summary>
    /// Tests that talking to the Archangel outside the blood castle is left to other handlers.
    /// </summary>
    [Test]
    public async ValueTask TalkingToArchangelOutsideBloodCastleIsNotHandledAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var eventArgs = new NpcTalkEventArgs();

        await CreatePlugIn()
            .PlayerTalksToNpcAsync(player, CreateNpc(player, BloodCastleArchangelTalkPlugIn.ArchangelNumber), eventArgs)
            .ConfigureAwait(false);

        Assert.That(eventArgs.HasBeenHandled, Is.False);
    }

    /// <summary>
    /// Tests that the plugin uses the configured NPC instead of the default one.
    /// </summary>
    [Test]
    public async ValueTask ConfiguredArchangelIsUsedAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.CurrentMiniGame = this.CreateBloodCastle(player);
        var plugIn = new BloodCastleArchangelTalkPlugIn
        {
            Configuration = new NpcTalkPlugInConfiguration { Npc = new MonsterDefinition { Number = OtherNpcNumber } },
        };
        var defaultArchangelArgs = new NpcTalkEventArgs();
        var configuredArchangelArgs = new NpcTalkEventArgs();

        await plugIn.PlayerTalksToNpcAsync(player, CreateNpc(player, BloodCastleArchangelTalkPlugIn.ArchangelNumber), defaultArchangelArgs).ConfigureAwait(false);
        await plugIn.PlayerTalksToNpcAsync(player, CreateNpc(player, OtherNpcNumber), configuredArchangelArgs).ConfigureAwait(false);

        Assert.That(defaultArchangelArgs.HasBeenHandled, Is.False);
        Assert.That(configuredArchangelArgs.HasBeenHandled, Is.True);
    }

    /// <summary>
    /// Tests that a player without the Archangel weapon can't claim another player's quest completion.
    /// </summary>
    [Test]
    public async ValueTask QuestHandInRequiresItemOwnershipAsync()
    {
        var owner = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var nonOwner = await PlayerTestHelper.CreatePlayerAsync(owner.GameContext).ConfigureAwait(false);
        var context = this.CreateBloodCastle(owner);
        await PrepareQuestHandInAsync(context).ConfigureAwait(false);
        var questItem = await AddQuestItemAsync(owner).ConfigureAwait(false);

        await context.TalkToNpcArchangelAsync(nonOwner).ConfigureAwait(false);

        Assert.That(owner.Inventory!.Items, Does.Contain(questItem));
        Assert.That(nonOwner.Inventory!.Items, Is.Empty);
        Mock.Get(nonOwner.ViewPlugIns.GetPlugIn<IShowDialogPlugIn>()!)
            .Verify(plugin => plugin.ShowDialogAsync(1, 0x18), Times.Once);
    }

    /// <summary>
    /// Tests that only the first quest-item owner can claim completion and consume an item.
    /// </summary>
    [Test]
    public async ValueTask DuplicateQuestHandInConsumesOnlyWinningItemAsync()
    {
        var winner = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var duplicateClaimant = await PlayerTestHelper.CreatePlayerAsync(winner.GameContext).ConfigureAwait(false);
        var context = this.CreateBloodCastle(winner);
        await PrepareQuestHandInAsync(context).ConfigureAwait(false);
        var winningItem = await AddQuestItemAsync(winner).ConfigureAwait(false);
        var duplicateItem = await AddQuestItemAsync(duplicateClaimant).ConfigureAwait(false);

        await context.TalkToNpcArchangelAsync(winner).ConfigureAwait(false);
        await context.TalkToNpcArchangelAsync(duplicateClaimant).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(winner.Inventory!.Items, Does.Not.Contain(winningItem));
            Assert.That(duplicateClaimant.Inventory!.Items, Does.Contain(duplicateItem));
        });
        Mock.Get(winner.ViewPlugIns.GetPlugIn<IShowDialogPlugIn>()!)
            .Verify(plugin => plugin.ShowDialogAsync(1, 0x17), Times.Once);
        Mock.Get(duplicateClaimant.ViewPlugIns.GetPlugIn<IShowDialogPlugIn>()!)
            .Verify(plugin => plugin.ShowDialogAsync(1, 0x2E), Times.Once);
    }

    private static BloodCastleArchangelTalkPlugIn CreatePlugIn()
    {
        return new BloodCastleArchangelTalkPlugIn
        {
            Configuration = new NpcTalkPlugInConfiguration { Npc = new MonsterDefinition { Number = BloodCastleArchangelTalkPlugIn.ArchangelNumber } },
        };
    }

    private static async ValueTask<Item> AddQuestItemAsync(Player player)
    {
        var item = new Item
        {
            Definition = new ItemDefinition { Group = 13, Number = 19 },
            ItemSlot = 12,
        };
        await player.Inventory!.AddItemAsync(item.ItemSlot, item).ConfigureAwait(false);
        return item;
    }

    private static async ValueTask PrepareQuestHandInAsync(BloodCastleContext context)
    {
        var players = (MiniGamePlayerRegistry)typeof(MiniGameContext)
            .GetField("_players", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(context)!;
        await players.SetStateAsync(MiniGameState.Playing).ConfigureAwait(false);

        var cycle = (BloodCastleCycleState)typeof(BloodCastleContext)
            .GetField("_cycleState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(context)!;
        cycle.TryRegisterGateDestroyed();
        cycle.TryRegisterStatueDestroyed();
    }

    private static NonPlayerCharacter CreateNpc(Player player, short number)
    {
        return new Mock<NonPlayerCharacter>(new MonsterSpawnArea(), new MonsterDefinition { Number = number }, player.CurrentMap!).Object;
    }

    private BloodCastleContext CreateBloodCastle(Player player)
    {
        var mapDefinition = new GameMapDefinition { Number = 11 };
        var definitionMock = new Mock<MiniGameDefinition>();
        definitionMock.SetupGet(d => d.Rewards).Returns(new List<MiniGameReward>());
        definitionMock.SetupGet(d => d.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definitionMock.SetupGet(d => d.Entrance).Returns(new ExitGate { Map = mapDefinition });
        var definition = definitionMock.Object;
        definition.Type = MiniGameType.BloodCastle;
        definition.EnterDuration = TimeSpan.FromMinutes(1);
        definition.GameDuration = TimeSpan.FromMinutes(5);
        definition.ExitDuration = TimeSpan.FromMinutes(1);
        definition.MaximumPlayerCount = 10;

        var mapInitializerMock = new Mock<IMapInitializer>();
        mapInitializerMock.Setup(m => m.CreateGameMap(It.IsAny<GameMapDefinition>()))
            .Returns<GameMapDefinition>(map => new GameMap(map, TimeSpan.FromMinutes(1), 16));
        var context = new BloodCastleContext(new MiniGameMapKey(mapDefinition.Number, definition.GameLevel, string.Empty), definition, player.GameContext, mapInitializerMock.Object);
        this._disposables.Add(context);
        return context;
    }
}
