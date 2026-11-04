// <copyright file="KanturuStateMachine.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kanturu;

using System.Threading;

/// <summary>
/// Executes the configured Kanturu phases in order and completes the event exactly once.
/// </summary>
internal sealed class KanturuStateMachine
{
    private readonly Func<KanturuPhaseDefinition, CancellationToken, Task<bool>> _runPhaseAsync;
    private readonly Func<CancellationToken, Task> _completeSuccessAsync;
    private readonly Action _completeFailure;

    /// <summary>
    /// Initializes a new instance of the <see cref="KanturuStateMachine"/> class.
    /// </summary>
    /// <param name="runPhaseAsync">Runs one configured phase.</param>
    /// <param name="completeSuccessAsync">Completes a successful event.</param>
    /// <param name="completeFailure">Completes a failed event.</param>
    public KanturuStateMachine(
        Func<KanturuPhaseDefinition, CancellationToken, Task<bool>> runPhaseAsync,
        Func<CancellationToken, Task> completeSuccessAsync,
        Action completeFailure)
    {
        this._runPhaseAsync = runPhaseAsync;
        this._completeSuccessAsync = completeSuccessAsync;
        this._completeFailure = completeFailure;
    }

    /// <summary>
    /// Executes a complete Maya and Nightmare cycle.
    /// </summary>
    /// <param name="phases">The configured phases in execution order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RunAsync(IEnumerable<KanturuPhaseDefinition> phases, CancellationToken cancellationToken)
    {
        foreach (var phase in phases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await this._runPhaseAsync(phase, cancellationToken).ConfigureAwait(false))
            {
                this._completeFailure();
                return false;
            }
        }

        await this._completeSuccessAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
