// <copyright file="MaximumConnectionsPerIpPlugInConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for the <see cref="MaximumConnectionsPerIpPlugIn"/>.
/// </summary>
public class MaximumConnectionsPerIpPlugInConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent connections per IP address.
    /// </summary>
    [Range(1, 100)]
    [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugInConfiguration_MaximumConnectionsPerIp_Caption), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
    public int MaximumConnectionsPerIp { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether excess connections are recorded without rejecting the login.
    /// </summary>
    [DefaultValue(true)]
    [Display(Name = "Observe only", Description = "Records multi-client violations without rejecting the login.")]
    public bool ObserveOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the central anti-cheat risk points for exceeding the connection limit.
    /// </summary>
    [DefaultValue(20.0)]
    [Range(0.1, 100.0)]
    [Display(Name = "Multi-client risk points")]
    public double RiskPoints { get; set; } = 20.0;
}
