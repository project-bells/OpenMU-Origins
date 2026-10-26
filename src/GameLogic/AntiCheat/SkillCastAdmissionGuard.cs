// <copyright file="SkillCastAdmissionGuard.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// Applies an independent token bucket to every skill used by one player.
/// </summary>
internal sealed class SkillCastAdmissionGuard
{
    private readonly object _sync = new();
    private readonly Dictionary<ushort, SkillBudget> _budgets = new();

    /// <summary>
    /// Tries to consume one server-side cast token.
    /// </summary>
    public bool TryAdmit(ushort skillId, DateTimeOffset now, double minimumIntervalMilliseconds, double burstCapacity)
    {
        lock (this._sync)
        {
            double interval = Math.Max(1.0, minimumIntervalMilliseconds);
            double capacity = Math.Max(1.0, burstCapacity);
            if (!this._budgets.TryGetValue(skillId, out var budget))
            {
                this._budgets[skillId] = new SkillBudget(capacity - 1.0, now);
                return true;
            }

            double elapsedMilliseconds = Math.Max(0, (now - budget.LastRefill).TotalMilliseconds);
            double tokens = Math.Min(capacity, budget.Tokens + (elapsedMilliseconds / interval));
            if (tokens >= 1.0)
            {
                this._budgets[skillId] = new SkillBudget(tokens - 1.0, now);
                return true;
            }

            this._budgets[skillId] = new SkillBudget(tokens, now);
            return false;
        }
    }

    private sealed record SkillBudget(double Tokens, DateTimeOffset LastRefill);
}
