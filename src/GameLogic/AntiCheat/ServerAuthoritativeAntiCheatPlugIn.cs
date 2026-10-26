// <copyright file="ServerAuthoritativeAntiCheatPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
}
