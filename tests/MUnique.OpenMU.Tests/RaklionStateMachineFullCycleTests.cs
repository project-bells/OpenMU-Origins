// <copyright file="RaklionStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Raklion;

/// <summary>
/// Deterministic full-cycle tests for the Raklion and Selupan state machine.
/// </summary>
[TestFixture]
public class RaklionStateMachineFullCycleTests
{
    private readonly RaklionEventDefinition _definition = new()
    {
        SelupanAppearanceDelay = TimeSpan.FromSeconds(5),
        HatcheryCloseDelay = TimeSpan.FromMinutes(5),
        PlayerRemovalDelay = TimeSpan.FromMinutes(2),
        HatcheryOpenDelay = TimeSpan.FromMinutes(10),
    };

    /// <summary>
    /// Tests the complete success cycle and its exactly-once terminal reward/result action.
    /// </summary>
    [Test]
    public void Success_RunsFullCycle_AndCompletesRewardExactlyOnce()
    {
        var actions = new[]
        {
            this.GetAction(RaklionState.End),
            this.GetAction(RaklionState.Idle),
            this.GetAction(RaklionState.Standby, this._definition.SelupanAppearanceDelay),
            this.GetAction(RaklionState.StartBattle, isSelupanDead: true),
            this.GetAction(RaklionState.Notify4, this._definition.PlayerRemovalDelay),
            this.GetAction(RaklionState.Notify4, this._definition.HatcheryOpenDelay, arePlayersRemoved: true),
            this.GetAction(RaklionState.End),
        };

        Assert.That(actions, Is.EqualTo(new[]
        {
            RaklionAction.Start,
            RaklionAction.CheckSpiderEggs,
            RaklionAction.SpawnSelupan,
            RaklionAction.EndBattleSuccess,
            RaklionAction.RemovePlayers,
            RaklionAction.Reopen,
            RaklionAction.Start,
        }));
        Assert.That(actions.Count(action => action == RaklionAction.EndBattleSuccess), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests timer progression through hatchery closing and failure when all battle players leave.
    /// </summary>
    [Test]
    public void TimeoutAndEmptyBattle_FailsWithoutSuccessReward()
    {
        var beforeTimeout = this.GetAction(RaklionState.StartBattle, this._definition.HatcheryCloseDelay - TimeSpan.FromMilliseconds(1));
        var atTimeout = this.GetAction(RaklionState.StartBattle, this._definition.HatcheryCloseDelay);
        var failed = this.GetAction(RaklionState.CloseDoor, areBattlePlayersEmpty: true);

        Assert.That(beforeTimeout, Is.EqualTo(RaklionAction.None));
        Assert.That(atTimeout, Is.EqualTo(RaklionAction.CloseHatchery));
        Assert.That(failed, Is.EqualTo(RaklionAction.EndBattleFailure));
    }

    /// <summary>
    /// Tests that Selupan death wins over the close timeout and empty-player failure races.
    /// </summary>
    [Test]
    public void SelupanDeath_WinsConcurrentTerminalRaces()
    {
        Assert.That(
            this.GetAction(RaklionState.StartBattle, this._definition.HatcheryCloseDelay, isSelupanDead: true),
            Is.EqualTo(RaklionAction.EndBattleSuccess));
        Assert.That(
            this.GetAction(RaklionState.CloseDoor, isSelupanDead: true, areBattlePlayersEmpty: true),
            Is.EqualTo(RaklionAction.EndBattleSuccess));
    }

    /// <summary>
    /// Tests that reset after failure returns to a fresh next scheduled cycle.
    /// </summary>
    [Test]
    public void Reset_AfterFailure_StartsNextCycle()
    {
        var failed = this.GetAction(RaklionState.CloseDoor, areBattlePlayersEmpty: true);
        var reopened = this.GetAction(RaklionState.Notify4, this._definition.HatcheryOpenDelay, arePlayersRemoved: true);
        var reset = this.GetAction(RaklionState.End);

        Assert.That(failed, Is.EqualTo(RaklionAction.EndBattleFailure));
        Assert.That(reopened, Is.EqualTo(RaklionAction.Reopen));
        Assert.That(reset, Is.EqualTo(RaklionAction.Start));
    }

    private RaklionAction GetAction(
        RaklionState state,
        TimeSpan elapsed = default,
        bool isSelupanDead = false,
        bool areBattlePlayersEmpty = false,
        bool arePlayersRemoved = false)
    {
        return RaklionStateMachine.GetAction(
            state,
            elapsed,
            this._definition,
            isSelupanDead,
            areBattlePlayersEmpty,
            arePlayersRemoved);
    }
}
