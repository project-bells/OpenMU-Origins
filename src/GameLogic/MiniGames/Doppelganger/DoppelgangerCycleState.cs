// <copyright file="DoppelgangerCycleState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Doppelganger;

using System.Threading;

/// <summary>
/// Tracks the terminal outcome of one Doppelganger cycle.
/// </summary>
internal sealed class DoppelgangerCycleState
{
    private const int Active = 0;
    private const int Succeeded = 1;
    private const int Failed = 2;

    private readonly int _maximumGoalCount;
    private int _goalCount;
    private int _outcome;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoppelgangerCycleState"/> class.
    /// </summary>
    /// <param name="maximumGoalCount">The number of monsters which causes the defense to fail.</param>
    public DoppelgangerCycleState(int maximumGoalCount)
    {
        this._maximumGoalCount = maximumGoalCount;
    }

    /// <summary>
    /// Gets a value indicating whether the defense failed.
    /// </summary>
    public bool IsDefenseFailed => Volatile.Read(ref this._outcome) == Failed;

    /// <summary>
    /// Gets the number of monsters which reached the magic circle, clamped for display.
    /// </summary>
    public int GoalCount => Math.Min(Volatile.Read(ref this._goalCount), this._maximumGoalCount);

    /// <summary>
    /// Registers a monster at the magic circle.
    /// </summary>
    /// <returns>The displayed goal count and whether this call completed the failure.</returns>
    public (int GoalCount, bool FailedNow) RegisterGoal()
    {
        var goalCount = Interlocked.Increment(ref this._goalCount);
        var failedNow = goalCount >= this._maximumGoalCount
                        && Interlocked.CompareExchange(ref this._outcome, Failed, Active) == Active;
        return (Math.Min(goalCount, this._maximumGoalCount), failedNow);
    }

    /// <summary>
    /// Completes the cycle successfully when its time limit elapsed.
    /// </summary>
    /// <returns><see langword="true"/> only for the call which completed the success.</returns>
    public bool TryCompleteSuccess() => Interlocked.CompareExchange(ref this._outcome, Succeeded, Active) == Active;
}
