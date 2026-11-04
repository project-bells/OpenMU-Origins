// <copyright file="ImperialGuardianStateMachine.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.ImperialGuardian;

using System.Threading;

/// <summary>
/// Executes the configured Imperial Guardian zones in order.
/// </summary>
internal sealed class ImperialGuardianStateMachine
{
    private readonly Func<int, CancellationToken, Task<bool>> _runZoneAsync;
    private readonly Func<ValueTask> _completeAsync;
    private readonly Func<ValueTask> _failAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImperialGuardianStateMachine"/> class.
    /// </summary>
    /// <param name="runZoneAsync">Runs one zone and reports whether it was cleared in time.</param>
    /// <param name="completeAsync">Completes a successful cycle.</param>
    /// <param name="failAsync">Completes a failed cycle.</param>
    public ImperialGuardianStateMachine(
        Func<int, CancellationToken, Task<bool>> runZoneAsync,
        Func<ValueTask> completeAsync,
        Func<ValueTask> failAsync)
    {
        this._runZoneAsync = runZoneAsync;
        this._completeAsync = completeAsync;
        this._failAsync = failAsync;
    }

    /// <summary>
    /// Runs a complete cycle.
    /// </summary>
    /// <param name="zoneCount">The number of configured zones.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when all zones were cleared; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RunAsync(int zoneCount, CancellationToken cancellationToken)
    {
        for (var zone = 0; zone < zoneCount; zone++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await this._runZoneAsync(zone, cancellationToken).ConfigureAwait(false))
            {
                await this._failAsync().ConfigureAwait(false);
                return false;
            }
        }

        await this._completeAsync().ConfigureAwait(false);
        return true;
    }
}
