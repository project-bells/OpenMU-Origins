// <copyright file="ServerAuthoritativeAntiCheatConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration of the server-authoritative anti-cheat admission controls.
/// </summary>
public sealed class ServerAuthoritativeAntiCheatConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether violations are recorded without rejecting packets.
    /// </summary>
    [DefaultValue(true)]
    [Display(Name = "Observe only", Description = "Records violations without rejecting packets or disconnecting players.")]
    public bool ObserveOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the sustained packet rate granted to a connection.
    /// </summary>
    [DefaultValue(80.0)]
    [Range(1.0, 10000.0)]
    [Display(Name = "Packets per second", Description = "Sustained packet budget for one game connection.")]
    public double PacketsPerSecond { get; set; } = 80.0;

    /// <summary>
    /// Gets or sets the maximum short packet burst.
    /// </summary>
    [DefaultValue(160.0)]
    [Range(1.0, 20000.0)]
    [Display(Name = "Packet burst capacity", Description = "Maximum short burst before packets are considered excessive.")]
    public double PacketBurstCapacity { get; set; } = 160.0;

    /// <summary>
    /// Gets or sets the risk points for each packet which exceeds the available budget.
    /// </summary>
    [DefaultValue(5.0)]
    [Range(0.1, 100.0)]
    [Display(Name = "Packet flood risk points")]
    public double PacketFloodRiskPoints { get; set; } = 5.0;

    /// <summary>
    /// Gets or sets the risk score half-life in minutes.
    /// </summary>
    [DefaultValue(AntiCheatRiskPolicy.DefaultHalfLifeMinutes)]
    [Range(0.1, 1440.0)]
    [Display(Name = "Risk score half-life (minutes)")]
    public double RiskScoreHalfLifeMinutes { get; set; } = AntiCheatRiskPolicy.DefaultHalfLifeMinutes;

    /// <summary>
    /// Gets or sets the maximum server-side range of a basic melee attack.
    /// </summary>
    [DefaultValue(3)]
    [Range(1, 16)]
    [Display(Name = "Basic attack range", Description = "Maximum tile distance for a basic attack.")]
    public int BasicAttackRange { get; set; } = 3;

    /// <summary>
    /// Gets or sets the additional range allowed for targeted skills to absorb normal position desynchronization.
    /// </summary>
    [DefaultValue(2)]
    [Range(0, 8)]
    [Display(Name = "Skill range tolerance", Description = "Additional tile tolerance for targeted and aimed skills.")]
    public int SkillRangeTolerance { get; set; } = 2;

    /// <summary>
    /// Gets or sets the risk points added for an impossible combat range.
    /// </summary>
    [DefaultValue(15.0)]
    [Range(0.1, 100.0)]
    [Display(Name = "Invalid combat range risk points")]
    public double InvalidCombatRangeRiskPoints { get; set; } = 15.0;
}
