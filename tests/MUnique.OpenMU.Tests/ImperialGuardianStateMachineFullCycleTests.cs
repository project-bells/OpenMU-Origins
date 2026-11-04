// <copyright file="ImperialGuardianStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Threading;
using MUnique.OpenMU.GameLogic.MiniGames.ImperialGuardian;

/// <summary>
/// Deterministic full-cycle tests for the Imperial Guardian zone state machine.
/// </summary>
[TestFixture]
public class ImperialGuardianStateMachineFullCycleTests
{
    /// <summary>
    /// Tests that all zones run in order and reward completion occurs exactly once.
    /// </summary>
    [Test]
    public async Task Success_RunsAllZonesInOrder_AndRewardsExactlyOnce()
    {
        var fixture = new StateMachineFixture();

        var succeeded = await fixture.RunAsync().ConfigureAwait(false);

        Assert.That(succeeded, Is.True);
        Assert.That(fixture.ExecutedZones, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(fixture.RewardCompletions, Is.EqualTo(1));
        Assert.That(fixture.Failures, Is.Zero);
    }

    /// <summary>
    /// Tests that a zone timeout stops later zones and never delivers the success reward.
    /// </summary>
    [Test]
    public async Task ZoneTimeout_StopsCycleWithoutReward()
    {
        var fixture = new StateMachineFixture(timedOutZone: 2);

        var succeeded = await fixture.RunAsync().ConfigureAwait(false);

        Assert.That(succeeded, Is.False);
        Assert.That(fixture.ExecutedZones, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(fixture.RewardCompletions, Is.Zero);
        Assert.That(fixture.Failures, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that an immediate first-zone timeout completes failure only once.
    /// </summary>
    [Test]
    public async Task FirstZoneTimeout_CompletesFailureExactlyOnce()
    {
        var fixture = new StateMachineFixture(timedOutZone: 0);

        Assert.That(await fixture.RunAsync().ConfigureAwait(false), Is.False);
        Assert.That(fixture.ExecutedZones, Is.EqualTo(new[] { 0 }));
        Assert.That(fixture.Failures, Is.EqualTo(1));
        Assert.That(fixture.RewardCompletions, Is.Zero);
    }

    /// <summary>
    /// Tests that reset after a timeout starts a fresh full cycle without duplicating its reward.
    /// </summary>
    [Test]
    public async Task Reset_AfterTimeout_AllowsNextFullCycle_WithoutDuplicateReward()
    {
        var timedOut = new StateMachineFixture(timedOutZone: 1);
        var reset = new StateMachineFixture();

        Assert.That(await timedOut.RunAsync().ConfigureAwait(false), Is.False);
        Assert.That(await reset.RunAsync().ConfigureAwait(false), Is.True);

        Assert.That(timedOut.RewardCompletions + reset.RewardCompletions, Is.EqualTo(1));
        Assert.That(reset.ExecutedZones, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(reset.Failures, Is.Zero);
    }

    private sealed class StateMachineFixture
    {
        private readonly int? _timedOutZone;
        private readonly ImperialGuardianStateMachine _stateMachine;

        public StateMachineFixture(int? timedOutZone = null)
        {
            this._timedOutZone = timedOutZone;
            this._stateMachine = new ImperialGuardianStateMachine(this.RunZoneAsync, this.CompleteAsync, this.FailAsync);
        }

        public List<int> ExecutedZones { get; } = new();

        public int RewardCompletions { get; private set; }

        public int Failures { get; private set; }

        public Task<bool> RunAsync() => this._stateMachine.RunAsync(4, CancellationToken.None);

        private Task<bool> RunZoneAsync(int zone, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ExecutedZones.Add(zone);
            return Task.FromResult(zone != this._timedOutZone);
        }

        private ValueTask CompleteAsync()
        {
            this.RewardCompletions++;
            return ValueTask.CompletedTask;
        }

        private ValueTask FailAsync()
        {
            this.Failures++;
            return ValueTask.CompletedTask;
        }
    }
}
