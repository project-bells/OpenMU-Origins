// <copyright file="DoppelgangerStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.MiniGames.Doppelganger;

/// <summary>
/// Deterministic full-cycle tests for the Doppelganger outcome state machine.
/// </summary>
[TestFixture]
public class DoppelgangerStateMachineFullCycleTests
{
    /// <summary>
    /// Tests that surviving until the time limit completes success and enables its reward exactly once.
    /// </summary>
    [Test]
    public void TimeoutSuccess_CompletesRewardExactlyOnce()
    {
        var cycle = new DoppelgangerCycleState(3);

        Assert.That(cycle.TryCompleteSuccess(), Is.True);
        Assert.That(cycle.TryCompleteSuccess(), Is.False);
        Assert.That(cycle.IsDefenseFailed, Is.False);
    }

    /// <summary>
    /// Tests that reaching the goal threshold ends the defense without enabling a success reward.
    /// </summary>
    [Test]
    public void DefenseFailure_FailsAtThreshold_WithoutReward()
    {
        var cycle = new DoppelgangerCycleState(3);

        Assert.That(cycle.RegisterGoal(), Is.EqualTo((1, false)));
        Assert.That(cycle.RegisterGoal(), Is.EqualTo((2, false)));
        Assert.That(cycle.RegisterGoal(), Is.EqualTo((3, true)));
        Assert.That(cycle.IsDefenseFailed, Is.True);
        Assert.That(cycle.TryCompleteSuccess(), Is.False);
    }

    /// <summary>
    /// Tests that simultaneous monsters can complete failure only once and never exceed the displayed maximum.
    /// </summary>
    [Test]
    public void ConcurrentGoalArrivals_CompleteFailureExactlyOnce()
    {
        var cycle = new DoppelgangerCycleState(3);

        var results = ParallelEnumerable.Range(0, 20)
            .Select(_ => cycle.RegisterGoal())
            .ToList();

        Assert.That(results.Count(result => result.FailedNow), Is.EqualTo(1));
        Assert.That(results, Has.All.Matches<(int GoalCount, bool FailedNow)>(result => result.GoalCount <= 3));
        Assert.That(cycle.GoalCount, Is.EqualTo(3));
    }

    /// <summary>
    /// Tests that a reset starts an independent next cycle after a failed run.
    /// </summary>
    [Test]
    public void Reset_AfterFailure_AllowsNextCycleRewardOnce()
    {
        var failedCycle = new DoppelgangerCycleState(1);
        var resetCycle = new DoppelgangerCycleState(1);

        Assert.That(failedCycle.RegisterGoal().FailedNow, Is.True);
        Assert.That(failedCycle.TryCompleteSuccess(), Is.False);
        Assert.That(resetCycle.GoalCount, Is.Zero);
        Assert.That(resetCycle.TryCompleteSuccess(), Is.True);
        Assert.That(resetCycle.TryCompleteSuccess(), Is.False);
    }
}
