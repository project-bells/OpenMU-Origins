// <copyright file="AntiCheatReportingExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// Reports rejected server-authoritative actions to the central risk engine and audit log.
/// </summary>
public static class AntiCheatReportingExtensions
{
    /// <summary>
    /// Records an impossible action and returns the resulting decision.
    /// </summary>
    public static AntiCheatDecision? ReportRejectedAction(this Player player, AntiCheatCategory category, string code, string facts)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<ServerAuthoritativeAntiCheatPlugIn>() is not { Configuration: { } configuration } antiCheat)
        {
            return null;
        }

        var decision = antiCheat.Report(
            player,
            new AntiCheatEvidence(category, configuration.InvalidCombatRangeRiskPoints, code, facts, DateTimeOffset.UtcNow),
            true);
        player.Logger.LogWarning(
            "Anti-cheat rejected action. Category={Category}, Code={Code}, Facts={Facts}, Score={Score}, RiskLevel={RiskLevel}, Account={Account}, Character={Character}",
            category,
            code,
            facts,
            decision?.Score,
            decision?.Level,
            player.Account?.LoginName,
            player.SelectedCharacter?.Name);
        return decision;
    }
}
