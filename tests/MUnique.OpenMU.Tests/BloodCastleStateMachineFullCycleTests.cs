// <copyright file="BloodCastleStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Deterministic full-cycle tests for the Blood Castle state machine.
/// </summary>
[TestFixture]
public class BloodCastleStateMachineFullCycleTests
{
    /// <summary>
    /// Tests the ordered success cycle and exactly-once successful reward completion.
    /// </summary>
    [Test]
    public void Success_RunsAllMilestones_AndCompletesRewardExactlyOnce()
    {
        var cycle = new BloodCastleCycleState();

        Assert.That(cycle.TryRegisterGateDestroyed(), Is.True);
        Assert.That(cycle.TryRegisterStatueDestroyed(), Is.True);
        Assert.That(cycle.TryCompleteSuccessfully(), Is.True);
        Assert.That(cycle.TryCompleteSuccessfully(), Is.False);
        Assert.That(cycle.TryCompleteTimeout(), Is.False);
        Assert.That(cycle.Stage, Is.EqualTo(BloodCastleCycleStage.CompletedSuccessfully));
    }

    /// <summary>
    /// Tests timeout at every non-terminal milestone without enabling a success reward.
    /// </summary>
    [Test]
    public void Timeout_AtAnyMilestone_CompletesFailureExactlyOnce()
    {
        foreach (var timeoutStage in new[]
                 {
                     BloodCastleCycleStage.Monsters,
                     BloodCastleCycleStage.GateDestroyed,
                     BloodCastleCycleStage.StatueDestroyed,
                 })
        {
            var cycle = CreateAtStage(timeoutStage);

            Assert.That(cycle.TryCompleteTimeout(), Is.True, $"First timeout at {timeoutStage}");
            Assert.That(cycle.TryCompleteTimeout(), Is.False, $"Repeated timeout at {timeoutStage}");
            Assert.That(cycle.TryCompleteSuccessfully(), Is.False, $"Success after timeout at {timeoutStage}");
            Assert.That(cycle.Stage, Is.EqualTo(BloodCastleCycleStage.TimedOut));
        }
    }

    /// <summary>
    /// Tests that milestones can't be skipped and duplicate signals don't advance the cycle twice.
    /// </summary>
    [Test]
    public void Milestones_RequireOrder_AndAdvanceExactlyOnce()
    {
        var cycle = new BloodCastleCycleState();

        Assert.That(cycle.TryRegisterStatueDestroyed(), Is.False);
        Assert.That(cycle.TryCompleteSuccessfully(), Is.False);
        Assert.That(cycle.TryRegisterGateDestroyed(), Is.True);
        Assert.That(cycle.TryRegisterGateDestroyed(), Is.False);
        Assert.That(cycle.TryRegisterStatueDestroyed(), Is.True);
        Assert.That(cycle.TryRegisterStatueDestroyed(), Is.False);
    }

    /// <summary>
    /// Tests that concurrent terminal signals select one result exactly once.
    /// </summary>
    [Test]
    public void ConcurrentTerminalSignals_CompleteExactlyOnce()
    {
        var cycle = CreateAtStage(BloodCastleCycleStage.StatueDestroyed);

        var completions = ParallelEnumerable.Range(0, 20)
            .Select(index => index % 2 == 0 ? cycle.TryCompleteSuccessfully() : cycle.TryCompleteTimeout())
            .ToList();

        Assert.That(completions.Count(completed => completed), Is.EqualTo(1));
        Assert.That(cycle.Stage, Is.AnyOf(BloodCastleCycleStage.CompletedSuccessfully, BloodCastleCycleStage.TimedOut));
    }

    /// <summary>
    /// Tests that reset after timeout starts an independent cycle which can reward once.
    /// </summary>
    [Test]
    public void Reset_AfterTimeout_AllowsNextCycleRewardOnce()
    {
        var timedOut = new BloodCastleCycleState();
        var reset = new BloodCastleCycleState();

        Assert.That(timedOut.TryCompleteTimeout(), Is.True);
        Assert.That(reset.Stage, Is.EqualTo(BloodCastleCycleStage.Monsters));
        Assert.That(reset.TryRegisterGateDestroyed(), Is.True);
        Assert.That(reset.TryRegisterStatueDestroyed(), Is.True);
        Assert.That(reset.TryCompleteSuccessfully(), Is.True);
        Assert.That(reset.TryCompleteSuccessfully(), Is.False);
    }

    private static BloodCastleCycleState CreateAtStage(BloodCastleCycleStage stage)
    {
        var cycle = new BloodCastleCycleState();
        if (stage >= BloodCastleCycleStage.GateDestroyed)
        {
            cycle.TryRegisterGateDestroyed();
        }

        if (stage >= BloodCastleCycleStage.StatueDestroyed)
        {
            cycle.TryRegisterStatueDestroyed();
        }

        return cycle;
    }
}
