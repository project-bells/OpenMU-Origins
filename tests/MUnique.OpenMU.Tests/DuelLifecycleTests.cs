// <copyright file="DuelLifecycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;

/// <summary>
/// Tests multiplayer cleanup and re-entry behavior of duel rooms.
/// </summary>
[TestFixture]
public class DuelLifecycleTests
{
    /// <summary>
    /// Tests that a delayed duplicate cleanup cannot release a newly allocated room.
    /// </summary>
    [Test]
    public async ValueTask DelayedCleanupDoesNotReleaseReallocatedRoomAsync()
    {
        var (gameContext, manager) = CreateGameContext();
        var firstPlayer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var secondPlayer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var firstRoom = await manager.GetFreeDuelRoomAsync(firstPlayer, secondPlayer).ConfigureAwait(false);
        Assert.That(firstRoom, Is.Not.Null);
        var allocatedRoom = firstRoom!;
        await manager.GiveBackDuelRoomAsync(allocatedRoom).ConfigureAwait(false);

        var replacementRoom = await manager.GetFreeDuelRoomAsync(firstPlayer, secondPlayer).ConfigureAwait(false);
        await manager.GiveBackDuelRoomAsync(allocatedRoom).ConfigureAwait(false);

        Assert.That(manager.GetRoomByIndex(0), Is.SameAs(replacementRoom));
    }

    /// <summary>
    /// Tests that resetting a request clears both players and makes the room reusable.
    /// </summary>
    [Test]
    public async ValueTask RequestCleanupAllowsImmediateReentryAsync()
    {
        var (gameContext, manager) = CreateGameContext();
        var requester = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var opponent = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var room = await manager.GetFreeDuelRoomAsync(requester, opponent).ConfigureAwait(false);
        Assert.That(room, Is.Not.Null);
        requester.DuelRoom = room;
        opponent.DuelRoom = room;
        room!.State = DuelState.DuelRequested;

        await room.ResetAndDisposeAsync(GameLogic.Views.Duel.DuelStartResult.Refused).ConfigureAwait(false);
        var replacementRoom = await manager.GetFreeDuelRoomAsync(requester, opponent).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(requester.DuelRoom, Is.Null);
            Assert.That(opponent.DuelRoom, Is.Null);
            Assert.That(replacementRoom, Is.Not.Null);
        });
    }

    /// <summary>
    /// Tests that spectator cleanup removes membership, permits re-entry, and notifies remaining spectators safely.
    /// </summary>
    [Test]
    public async ValueTask SpectatorCleanupAllowsReentryAsync()
    {
        var (gameContext, manager) = CreateGameContext();
        var requester = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var opponent = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var leavingSpectator = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var remainingSpectator = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var room = await manager.GetFreeDuelRoomAsync(requester, opponent).ConfigureAwait(false);
        Assert.That(room, Is.Not.Null);
        room!.Spectators.Add(leavingSpectator);
        room.Spectators.Add(remainingSpectator);
        leavingSpectator.DuelRoom = room;
        remainingSpectator.DuelRoom = room;

        await room.RemoveSpectatorAsync(leavingSpectator).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(leavingSpectator.DuelRoom, Is.Null);
            Assert.That(room.Spectators, Is.EqualTo(new[] { remainingSpectator }));
            Assert.That(remainingSpectator.DuelRoom, Is.SameAs(room));
        });
    }

    private static (GameContext GameContext, DuelRoomManager Manager) CreateGameContext()
    {
        var configuration = PlayerTestHelper.CreateGameConfiguration();
        Mock.Get(configuration).Setup(c => c.MagicEffects).Returns(new List<MagicEffectDefinition>());
        var duelConfiguration = new Mock<DuelConfiguration>();
        duelConfiguration.Setup(c => c.DuelAreas).Returns(new List<DuelArea>
        {
            new() { Index = 0 },
        });
        configuration.DuelConfiguration = duelConfiguration.Object;
        var mapInitializer = new MapInitializer(configuration, Microsoft.Extensions.Logging.Abstractions.NullLogger<MapInitializer>.Instance, NullDropGenerator.Instance, null);
        var gameContext = new GameContext(
            configuration,
            new Persistence.InMemory.InMemoryPersistenceContextProvider(),
            mapInitializer,
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance,
            new PlugIns.PlugInManager(null, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, null, null),
            NullDropGenerator.Instance,
            new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameContext.PlugInManager;
        mapInitializer.PathFinderPool = gameContext.PathFinderPool;
        return (gameContext, gameContext.DuelRoomManager);
    }
}
