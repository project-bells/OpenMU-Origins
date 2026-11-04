// <copyright file="ChaosCastleCycleState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Threading;

/// <summary>
/// Tracks the start, early-end condition, and terminal result of one Chaos Castle cycle.
/// </summary>
internal sealed class ChaosCastleCycleState
{
    private int _stage = (int)ChaosCastleCycleStage.Waiting;

    /// <summary>
    /// Gets the current cycle stage.
    /// </summary>
    public ChaosCastleCycleStage Stage => (ChaosCastleCycleStage)Volatile.Read(ref this._stage);

    /// <summary>
    /// Starts the battle.
    /// </summary>
    /// <returns><see langword="true"/> exactly once when the cycle starts.</returns>
    public bool TryStart()
    {
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)ChaosCastleCycleStage.Playing,
            (int)ChaosCastleCycleStage.Waiting) == (int)ChaosCastleCycleStage.Waiting;
    }

    /// <summary>
    /// Determines whether the running battle reached an early terminal condition.
    /// </summary>
    /// <param name="alivePlayerCount">The number of living players.</param>
    /// <param name="aliveMonsterCount">The number of living monsters.</param>
    /// <returns><see langword="true"/> when nobody survived, or the last player defeated every monster.</returns>
    public bool ShouldFinishEarly(int alivePlayerCount, int aliveMonsterCount)
    {
        return this.Stage == ChaosCastleCycleStage.Playing
            && (alivePlayerCount <= 0 || (alivePlayerCount == 1 && aliveMonsterCount <= 0));
    }

    /// <summary>
    /// Completes the running battle after an early end or timeout.
    /// </summary>
    /// <param name="finisherCount">The number of surviving players eligible for ranking and rewards.</param>
    /// <returns><see langword="true"/> exactly once when the terminal result is selected.</returns>
    public bool TryComplete(int finisherCount)
    {
        var result = finisherCount > 0
            ? ChaosCastleCycleStage.CompletedWithWinner
            : ChaosCastleCycleStage.CompletedEmpty;
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)result,
            (int)ChaosCastleCycleStage.Playing) == (int)ChaosCastleCycleStage.Playing;
    }
}
