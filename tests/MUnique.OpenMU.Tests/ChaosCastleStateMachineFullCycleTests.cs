// <copyright file="ChaosCastleStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Deterministic full-cycle tests for the Chaos Castle state machine.
/// </summary>
[TestFixture]
public class ChaosCastleStateMachineFullCycleTests
{
    /// <summary>
    /// Tests that the last survivor wins early after all monsters are defeated and receives rewards once.
    /// </summary>
    [Test]
    public void LastSurvivorSuccess_CompletesRewardExactlyOnce()
    {
        var cycle = new ChaosCastleCycleState();

        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.ShouldFinishEarly(1, 1), Is.False);
        Assert.That(cycle.ShouldFinishEarly(1, 0), Is.True);
        Assert.That(cycle.TryComplete(1), Is.True);
        Assert.That(cycle.TryComplete(1), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(ChaosCastleCycleStage.CompletedWithWinner));
    }

    /// <summary>
    /// Tests that timeout with multiple survivors selects the ranked winner and rewards once.
    /// </summary>
    [Test]
    public void TimeoutWithSurvivors_CompletesRankedRewardExactlyOnce()
    {
        var cycle = new ChaosCastleCycleState();

        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.ShouldFinishEarly(3, 20), Is.False);
        Assert.That(cycle.TryComplete(3), Is.True);
        Assert.That(cycle.TryComplete(3), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(ChaosCastleCycleStage.CompletedWithWinner));
    }

    /// <summary>
    /// Tests that losing all players ends early without enabling a winner reward.
    /// </summary>
    [Test]
    public void AllPlayersLost_CompletesWithoutReward()
    {
        var cycle = new ChaosCastleCycleState();

        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.ShouldFinishEarly(0, 10), Is.True);
        Assert.That(cycle.TryComplete(0), Is.True);
        Assert.That(cycle.TryComplete(1), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(ChaosCastleCycleStage.CompletedEmpty));
    }

    /// <summary>
    /// Tests that transitions can't run out of order or more than once.
    /// </summary>
    [Test]
    public void Transitions_RequireOrder_AndAdvanceExactlyOnce()
    {
        var cycle = new ChaosCastleCycleState();

        Assert.That(cycle.ShouldFinishEarly(0, 0), Is.False);
        Assert.That(cycle.TryComplete(1), Is.False);
        Assert.That(cycle.TryStart(), Is.True);
        Assert.That(cycle.TryStart(), Is.False);
        Assert.That(cycle.TryComplete(1), Is.True);
        Assert.That(cycle.ShouldFinishEarly(0, 0), Is.False);
    }

    /// <summary>
    /// Tests that competing timeout and early-end results complete only once.
    /// </summary>
    [Test]
    public void ConcurrentTerminalSignals_CompleteExactlyOnce()
    {
        var cycle = new ChaosCastleCycleState();
        cycle.TryStart();

        var completions = ParallelEnumerable.Range(0, 20)
            .Select(index => cycle.TryComplete(index % 2))
            .ToList();

        Assert.That(completions.Count(completed => completed), Is.EqualTo(1));
        Assert.That(cycle.Stage, Is.AnyOf(ChaosCastleCycleStage.CompletedWithWinner, ChaosCastleCycleStage.CompletedEmpty));
    }

    /// <summary>
    /// Tests that reset after an empty cycle starts an independent rewardable cycle.
    /// </summary>
    [Test]
    public void Reset_AfterEmptyCycle_AllowsNextCycleRewardOnce()
    {
        var emptyCycle = new ChaosCastleCycleState();
        var resetCycle = new ChaosCastleCycleState();

        Assert.That(emptyCycle.TryStart(), Is.True);
        Assert.That(emptyCycle.TryComplete(0), Is.True);
        Assert.That(resetCycle.Stage, Is.EqualTo(ChaosCastleCycleStage.Waiting));
        Assert.That(resetCycle.TryStart(), Is.True);
        Assert.That(resetCycle.TryComplete(1), Is.True);
        Assert.That(resetCycle.TryComplete(1), Is.False);
    }
}
