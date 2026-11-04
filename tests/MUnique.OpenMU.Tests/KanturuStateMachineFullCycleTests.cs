// <copyright file="KanturuStateMachineFullCycleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Threading;
using MUnique.OpenMU.GameLogic.MiniGames.Kanturu;

/// <summary>
/// Deterministic full-cycle tests for the Kanturu Maya and Nightmare state machine.
/// </summary>
[TestFixture]
public class KanturuStateMachineFullCycleTests
{
    /// <summary>
    /// Tests the successful Maya-to-Nightmare cycle, including exactly-once reward completion.
    /// </summary>
    [Test]
    public async Task Success_RunsMayaAndNightmareInOrder_AndRewardsExactlyOnce()
    {
        var fixture = new StateMachineFixture();

        var succeeded = await fixture.RunAsync().ConfigureAwait(false);

        Assert.That(succeeded, Is.True);
        Assert.That(fixture.ExecutedPhases, Is.EqualTo(new[] { "Maya 1", "Maya 2", "Maya 3", "Transition", "Nightmare" }));
        Assert.That(fixture.RewardDeliveries, Is.EqualTo(1));
        Assert.That(fixture.Failures, Is.Zero);
    }

    /// <summary>
    /// Tests that a Maya timeout ends the cycle without entering Nightmare or delivering a reward.
    /// </summary>
    [Test]
    public async Task MayaTimeout_StopsBeforeNightmare_AndResetsWithoutReward()
    {
        var fixture = new StateMachineFixture("Maya 2");

        var succeeded = await fixture.RunAsync().ConfigureAwait(false);

        Assert.That(succeeded, Is.False);
        Assert.That(fixture.ExecutedPhases, Is.EqualTo(new[] { "Maya 1", "Maya 2" }));
        Assert.That(fixture.RewardDeliveries, Is.Zero);
        Assert.That(fixture.Failures, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that a Nightmare timeout ends the cycle without delivering the success reward.
    /// </summary>
    [Test]
    public async Task NightmareTimeout_CompletesFailureWithoutReward()
    {
        var fixture = new StateMachineFixture("Nightmare");

        var succeeded = await fixture.RunAsync().ConfigureAwait(false);

        Assert.That(succeeded, Is.False);
        Assert.That(fixture.ExecutedPhases, Is.EqualTo(new[] { "Maya 1", "Maya 2", "Maya 3", "Transition", "Nightmare" }));
        Assert.That(fixture.RewardDeliveries, Is.Zero);
        Assert.That(fixture.Failures, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that a reset creates an independent next cycle without retaining a timed-out phase.
    /// </summary>
    [Test]
    public async Task Reset_AfterTimeout_AllowsNextFullCycle_WithoutDuplicateReward()
    {
        var timedOut = new StateMachineFixture("Nightmare");
        var reset = new StateMachineFixture();

        Assert.That(await timedOut.RunAsync().ConfigureAwait(false), Is.False);
        Assert.That(await reset.RunAsync().ConfigureAwait(false), Is.True);

        Assert.That(timedOut.RewardDeliveries + reset.RewardDeliveries, Is.EqualTo(1));
        Assert.That(reset.ExecutedPhases, Is.EqualTo(new[] { "Maya 1", "Maya 2", "Maya 3", "Transition", "Nightmare" }));
        Assert.That(reset.Failures, Is.Zero);
    }

    private sealed class StateMachineFixture
    {
        private readonly string? _timedOutPhase;
        private readonly KanturuStateMachine _stateMachine;

        public StateMachineFixture(string? timedOutPhase = null)
        {
            this._timedOutPhase = timedOutPhase;
            this._stateMachine = new KanturuStateMachine(this.RunPhaseAsync, this.DeliverRewardAsync, () => this.Failures++);
        }

        public List<string> ExecutedPhases { get; } = new();

        public int RewardDeliveries { get; private set; }

        public int Failures { get; private set; }

        public Task<bool> RunAsync() => this._stateMachine.RunAsync(CreatePhases(), CancellationToken.None);

        private static IEnumerable<KanturuPhaseDefinition> CreatePhases()
        {
            yield return CreatePhase("Maya 1", KanturuPhaseKind.MonsterWave);
            yield return CreatePhase("Maya 2", KanturuPhaseKind.MonsterWave);
            yield return CreatePhase("Maya 3", KanturuPhaseKind.MonsterWave);
            yield return CreatePhase("Transition", KanturuPhaseKind.Transition);
            yield return CreatePhase("Nightmare", KanturuPhaseKind.Nightmare);
        }

        private static KanturuPhaseDefinition CreatePhase(string name, KanturuPhaseKind kind) => new() { Name = name, Kind = kind };

        private Task<bool> RunPhaseAsync(KanturuPhaseDefinition phase, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ExecutedPhases.Add(phase.Name);
            return Task.FromResult(phase.Name != this._timedOutPhase);
        }

        private Task DeliverRewardAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.RewardDeliveries++;
            return Task.CompletedTask;
        }
    }
}
