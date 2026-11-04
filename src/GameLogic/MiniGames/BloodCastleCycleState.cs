// <copyright file="BloodCastleCycleState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Threading;

/// <summary>
/// Tracks the ordered milestones and terminal result of one Blood Castle cycle.
/// </summary>
internal sealed class BloodCastleCycleState
{
    private int _stage = (int)BloodCastleCycleStage.Monsters;

    /// <summary>
    /// Gets the current cycle stage.
    /// </summary>
    public BloodCastleCycleStage Stage => (BloodCastleCycleStage)Volatile.Read(ref this._stage);

    /// <summary>
    /// Gets a value indicating whether the castle gate was destroyed in this cycle.
    /// </summary>
    public bool IsGateDestroyed => this.Stage >= BloodCastleCycleStage.GateDestroyed;

    /// <summary>
    /// Registers the destruction of the castle gate.
    /// </summary>
    /// <returns><see langword="true"/> when this call advanced the cycle; otherwise <see langword="false"/>.</returns>
    public bool TryRegisterGateDestroyed()
    {
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)BloodCastleCycleStage.GateDestroyed,
            (int)BloodCastleCycleStage.Monsters) == (int)BloodCastleCycleStage.Monsters;
    }

    /// <summary>
    /// Registers the destruction of the statue after the gate.
    /// </summary>
    /// <returns><see langword="true"/> when this call advanced the cycle; otherwise <see langword="false"/>.</returns>
    public bool TryRegisterStatueDestroyed()
    {
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)BloodCastleCycleStage.StatueDestroyed,
            (int)BloodCastleCycleStage.GateDestroyed) == (int)BloodCastleCycleStage.GateDestroyed;
    }

    /// <summary>
    /// Completes the cycle successfully after the quest item was returned.
    /// </summary>
    /// <returns><see langword="true"/> exactly once for a successful cycle.</returns>
    public bool TryCompleteSuccessfully()
    {
        return Interlocked.CompareExchange(
            ref this._stage,
            (int)BloodCastleCycleStage.CompletedSuccessfully,
            (int)BloodCastleCycleStage.StatueDestroyed) == (int)BloodCastleCycleStage.StatueDestroyed;
    }

    /// <summary>
    /// Completes the cycle as timed out unless it already reached a terminal result.
    /// </summary>
    /// <returns><see langword="true"/> exactly once for a timed-out cycle.</returns>
    public bool TryCompleteTimeout()
    {
        while (true)
        {
            var stage = this.Stage;
            if (stage is BloodCastleCycleStage.CompletedSuccessfully or BloodCastleCycleStage.TimedOut)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref this._stage, (int)BloodCastleCycleStage.TimedOut, (int)stage) == (int)stage)
            {
                return true;
            }
        }
    }
}
