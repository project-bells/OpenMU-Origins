// <copyright file="ServerAuthoritativeAntiCheatPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Provides configurable server-authoritative anti-cheat controls.
/// </summary>
[PlugIn]
[Display(Name = "Server-Authoritative Anti-Cheat", Description = "Central risk scoring and packet admission controls.")]
[Guid("4DD0325C-1C78-49AF-A271-F0A736D10E8C")]
public sealed class ServerAuthoritativeAntiCheatPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ServerAuthoritativeAntiCheatConfiguration>, ISupportDefaultCustomConfiguration
{
    private readonly ConditionalWeakTable<Player, AntiCheatRiskState> _riskStates = new();
    private readonly ConditionalWeakTable<Player, SkillCastAdmissionGuard> _skillCastGuards = new();
    private readonly ConditionalWeakTable<Player, EconomicTransactionState> _economicTransactionStates = new();

    /// <inheritdoc />
    public ServerAuthoritativeAntiCheatConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ServerAuthoritativeAntiCheatConfiguration();

    /// <summary>
    /// Adds normalized evidence to the central risk score of a player session.
    /// </summary>
    /// <param name="player">The player whose server-observed action produced the evidence.</param>
    /// <param name="evidence">The normalized evidence.</param>
    /// <param name="impossibleAction">Whether the current action violates a hard server invariant.</param>
    /// <returns>The resulting decision, or <c>null</c> when the plugin has no configuration.</returns>
    public AntiCheatDecision? Report(Player player, AntiCheatEvidence evidence, bool impossibleAction)
    {
        if (this.Configuration is not { } configuration)
        {
            return null;
        }

        var decision = this._riskStates.GetOrCreateValue(player).Add(evidence, impossibleAction, configuration.RiskScoreHalfLifeMinutes);
        player.Logger.LogWarning(
            "Anti-cheat evidence. Category={Category}, Code={Code}, Facts={Facts}, Points={Points}, Score={Score}, RiskLevel={RiskLevel}, RejectAction={RejectAction}, Disconnect={Disconnect}, ManualReview={ManualReview}, Account={Account}, Character={Character}",
            evidence.Category,
            evidence.Code,
            evidence.Facts,
            evidence.Points,
            decision.Score,
            decision.Level,
            decision.RejectAction,
            decision.Disconnect,
            decision.RequiresManualReview,
            player.Account?.LoginName,
            player.SelectedCharacter?.Name);
        return decision;
    }

    /// <summary>
    /// Checks a skill cast against the server-side per-skill timing budget.
    /// </summary>
    public bool TryAdmitSkillCast(Player player, ushort skillId, DateTimeOffset now, out AntiCheatDecision? decision)
    {
        if (this.Configuration is not { } configuration
            || player is Offline.OfflinePlayer
            || this._skillCastGuards.GetOrCreateValue(player).TryAdmit(
                skillId,
                now,
                configuration.MinimumSkillIntervalMilliseconds,
                configuration.SkillCastBurstCapacity))
        {
            decision = null;
            return true;
        }

        decision = this.Report(
            player,
            new AntiCheatEvidence(
                AntiCheatCategory.SkillCooldown,
                configuration.SkillCooldownRiskPoints,
                "combat.skill_cast.rate_exceeded",
                $"skill={skillId};minimumIntervalMs={configuration.MinimumSkillIntervalMilliseconds:F2};burst={configuration.SkillCastBurstCapacity:F2}",
                now),
            true);
        return configuration.ObserveOnly;
    }

    /// <summary>
    /// Tries to reserve the player's economic transaction slot. A player may mutate inventory or
    /// currency through only one client-requested transaction at a time.
    /// </summary>
    /// <param name="player">The player which requested the transaction.</param>
    /// <param name="operation">A stable operation name for the audit evidence.</param>
    /// <param name="now">The server timestamp of the request.</param>
    /// <param name="lease">A lease which must be disposed after the transaction finishes.</param>
    /// <param name="decision">The risk decision when the request overlaps another transaction.</param>
    /// <returns><c>true</c> when the transaction slot was reserved; otherwise, <c>false</c>.</returns>
    public bool TryBeginEconomicTransaction(Player player, string operation, DateTimeOffset now, out IDisposable? lease, out AntiCheatDecision? decision)
    {
        var state = this._economicTransactionStates.GetOrCreateValue(player);
        if (state.TryEnter(out lease))
        {
            decision = null;
            return true;
        }

        decision = this.Report(
            player,
            new AntiCheatEvidence(
                AntiCheatCategory.TradeIntegrity,
                this.Configuration?.ConcurrentTransactionRiskPoints ?? 25.0,
                "economy.transaction.concurrent",
                $"operation={operation}",
                now),
            true);
        return false;
    }

    private sealed class EconomicTransactionState
    {
        private readonly object _sync = new();
        private bool _active;

        public bool TryEnter(out IDisposable? lease)
        {
            lock (this._sync)
            {
                if (this._active)
                {
                    lease = null;
                    return false;
                }

                this._active = true;
                lease = new TransactionLease(this);
                return true;
            }
        }

        private void Exit()
        {
            lock (this._sync)
            {
                this._active = false;
            }
        }

        private sealed class TransactionLease : IDisposable
        {
            private EconomicTransactionState? _owner;

            public TransactionLease(EconomicTransactionState owner)
            {
                this._owner = owner;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref this._owner, null)?.Exit();
            }
        }
    }
}
