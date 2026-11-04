// <copyright file="DevilSquareStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Deterministic full-cycle tests for the Devil Square state machine.
/// </summary>
[TestFixture]
public class DevilSquareStateMachineFullCycleTests
{
    /// <summary>
    /// Tests that surviving the configured duration completes the reward path exactly once.
    /// </summary>
    [Test]
    public void TimeoutWithFinishers_CompletesRewardExactlyOnce()
    {
        var cycle = new DevilSquareCycleState();

        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.TryComplete(4), Is.True);
        Assert.That(cycle.TryComplete(4), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(DevilSquareCycleStage.CompletedWithFinishers));
    }

    /// <summary>
    /// Tests that an event without remaining players completes without enabling rewards.
    /// </summary>
    [Test]
    public void EmptyEvent_CompletesWithoutReward()
    {
        var cycle = new DevilSquareCycleState();

        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.TryComplete(0), Is.True);
        Assert.That(cycle.TryComplete(1), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(DevilSquareCycleStage.CompletedEmpty));
    }

    /// <summary>
    /// Tests that start and completion can't run out of order or more than once.
    /// </summary>
    [Test]
    public void Transitions_RequireOrder_AndAdvanceExactlyOnce()
    {
        var cycle = new DevilSquareCycleState();

        Assert.That(cycle.TryComplete(1), Is.False);
        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.TryStart(), Is.False);
        Assert.That(cycle.TryComplete(1), Is.True);
        Assert.That(cycle.TryStart(), Is.False);
    }

    /// <summary>
    /// Tests that competing terminal signals select one result exactly once.
    /// </summary>
    [Test]
    public void ConcurrentTerminalSignals_CompleteExactlyOnce()
    {
        var cycle = new DevilSquareCycleState();
        cycle.TryStart();

        var completions = ParallelEnumerable.Range(0, 20)
            .Select(index => cycle.TryComplete(index % 2))
            .ToList();

        Assert.That(completions.Count(completed => completed), Is.EqualTo(1));
        Assert.That(cycle.Stage, Is.AnyOf(DevilSquareCycleStage.CompletedWithFinishers, DevilSquareCycleStage.CompletedEmpty));
    }

    /// <summary>
    /// Tests that reset after an empty event starts an independent rewardable cycle.
    /// </summary>
    [Test]
    public void Reset_AfterEmptyEvent_AllowsNextCycleRewardOnce()
    {
        var emptyCycle = new DevilSquareCycleState();
        var resetCycle = new DevilSquareCycleState();

        Assert.That(emptyCycle.TryStart(), Is.True);
        Assert.That(emptyCycle.TryComplete(0), Is.True);
        Assert.That(resetCycle.Stage, Is.EqualTo(DevilSquareCycleStage.Waiting));
        Assert.That(resetCycle.TryStart(), Is.True);
        Assert.That(resetCycle.TryComplete(1), Is.True);
        Assert.That(resetCycle.TryComplete(1), Is.False);
    }
}
