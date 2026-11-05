// <copyright file="MuHelperLifecycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Threading;
using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MuHelper;
using MUnique.OpenMU.GameLogic.Views.MuHelper;

/// <summary>
/// Integration tests for the online MU Helper lifecycle and economy.
/// </summary>
[TestFixture]
public class MuHelperLifecycleTests
{
    /// <summary>
    /// Tests the inclusive minimum and maximum level boundaries and both rejection paths.
    /// </summary>
    /// <param name="level">The character level.</param>
    /// <param name="expected">Whether the helper should start.</param>
    [TestCase(9, false)]
    [TestCase(10, true)]
    [TestCase(20, true)]
    [TestCase(21, false)]
    public async Task LevelBoundariesAreEnforcedAsync(int level, bool expected)
    {
        var player = await CreatePlayerAsync(new MuHelperConfiguration
        {
            MinLevel = 10,
            MaxLevel = 20,
            CostPerStage = [0],
        }).ConfigureAwait(false);
        player.Attributes![Stats.Level] = level;

        var started = await player.MuHelper.TryStartAsync().ConfigureAwait(false);

        Assert.That(started, Is.EqualTo(expected));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperStatusUpdatePlugIn>()!)
            .Verify(view => view.StartAsync(), expected ? Times.Once() : Times.Never());
        await player.MuHelper.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Tests the initial charge and the first recurring charge at the configured interval.
    /// </summary>
    [Test]
    public async Task ChargesInitiallyAndAtRecurringIntervalAsync()
    {
        var player = await CreatePlayerAsync(new MuHelperConfiguration
        {
            MinLevel = 1,
            MaxLevel = 400,
            CostPerStage = [2],
            PayInterval = TimeSpan.FromMilliseconds(20),
        }).ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        player.Money = 100;
        var secondCharge = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var chargeCount = 0;
        Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperStatusUpdatePlugIn>()!)
            .Setup(view => view.ConsumeMoneyAsync(20))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref chargeCount) == 2)
                {
                    secondCharge.TrySetResult();
                }
            })
            .Returns(ValueTask.CompletedTask);

        Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.True);
        await secondCharge.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        Assert.That(player.Money, Is.EqualTo(60));
        await player.MuHelper.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that a failed recurring charge stops once without deadlocking.
    /// </summary>
    [Test]
    public async Task InsufficientZenStopsAndNotifiesOnceWithoutDeadlockAsync()
    {
        var player = await CreatePlayerAsync(new MuHelperConfiguration
        {
            MinLevel = 1,
            MaxLevel = 400,
            CostPerStage = [2],
            PayInterval = TimeSpan.FromMilliseconds(20),
        }).ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        player.Money = 20;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var statusView = Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperStatusUpdatePlugIn>()!);
        statusView.Setup(view => view.StopAsync()).Callback(() => stopped.TrySetResult()).Returns(ValueTask.CompletedTask);

        Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.True);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        await player.MuHelper.StopAsync().ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(player.Money, Is.Zero);
            Assert.That(player.Attributes[Stats.IsMuHelperActive], Is.Zero);
            statusView.Verify(view => view.StopAsync(), Times.Once());
        });
    }

    /// <summary>
    /// Tests that concurrent stop, disconnect cleanup, and disposal share one shutdown.
    /// </summary>
    [Test]
    public async Task ConcurrentStopAndDisposeCallsCompleteOnceAsync()
    {
        var player = await CreatePlayerAsync(new MuHelperConfiguration
        {
            MinLevel = 1,
            MaxLevel = 400,
            CostPerStage = [0],
            PayInterval = TimeSpan.FromMinutes(5),
        }).ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var statusView = Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperStatusUpdatePlugIn>()!);
        Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.True);

        await Task.WhenAll(
            player.MuHelper.StopAsync().AsTask(),
            player.MuHelper.StopAsync().AsTask(),
            player.MuHelper.DisposeAsync().AsTask()).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        statusView.Verify(view => view.StopAsync(), Times.Once());
        Assert.That(player.Attributes[Stats.IsMuHelperActive], Is.Zero);
    }

    private static async ValueTask<Player> CreatePlayerAsync(MuHelperConfiguration configuration)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new MuHelperFeaturePlugIn { Configuration = configuration }, true);
        return player;
    }
}
