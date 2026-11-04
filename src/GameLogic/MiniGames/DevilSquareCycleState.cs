// <copyright file="DevilSquareCycleState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Threading;

/// <summary>
/// Arbitrates the start and terminal result of one Devil Square cycle.
/// </summary>
internal sealed class DevilSquareCycleState
{
    private int _stage = (int)DevilSquareCycleStage.Waiting;

    /// <summary>
    /// Gets the current cycle stage.
    /// </summary>
    public DevilSquareCycleStage Stage => (DevilSquareCycleStage)Volatile.Read(ref this._stage);

    /// <summary>
    /// Starts the battle.
    /// </summary>
    /// <returns><see langword="true"/> exactly once when the cycle starts.</returns>
    public bool TryStart()
    {
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)DevilSquareCycleStage.Playing,
            (int)DevilSquareCycleStage.Waiting) == (int)DevilSquareCycleStage.Waiting;
    }

    /// <summary>
    /// Completes the running battle.
    /// </summary>
    /// <param name="finisherCount">The number of players which survived until completion.</param>
    /// <returns><see langword="true"/> exactly once when the terminal result is selected.</returns>
    public bool TryComplete(int finisherCount)
    {
        var result = finisherCount > 0
            ? DevilSquareCycleStage.CompletedWithFinishers
            : DevilSquareCycleStage.CompletedEmpty;
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)result,
            (int)DevilSquareCycleStage.Playing) == (int)DevilSquareCycleStage.Playing;
    }
}
