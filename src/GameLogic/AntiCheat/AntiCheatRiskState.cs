// <copyright file="AntiCheatRiskState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// Thread-safe in-memory risk accumulator for one connection/player session.
/// Durable evidence storage is deliberately handled by an audit sink, not this state object.
/// </summary>
public sealed class AntiCheatRiskState
{
    private readonly object _sync = new();
    private readonly HashSet<AntiCheatCategory> _categories = new();
    private DateTimeOffset? _lastObservation;
    private double _score;

    /// <summary>
    /// Adds evidence and returns the decision for the current action.
    /// </summary>
    public AntiCheatDecision Add(AntiCheatEvidence evidence, bool impossibleAction, double halfLifeMinutes = AntiCheatRiskPolicy.DefaultHalfLifeMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence.Code);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(evidence.Points);

        lock (this._sync)
        {
            if (this._lastObservation is { } previous && evidence.ObservedAt < previous)
            {
                throw new ArgumentException("Evidence timestamps must be monotonic for a session.", nameof(evidence));
            }

            if (this._lastObservation is { } last)
            {
                this._score = AntiCheatRiskPolicy.Decay(this._score, evidence.ObservedAt - last, halfLifeMinutes);
            }

            this._score += evidence.Points;
            this._lastObservation = evidence.ObservedAt;
            this._categories.Add(evidence.Category);
            return AntiCheatRiskPolicy.Decide(this._score, this._categories.Count, impossibleAction);
        }
    }
}
