// <copyright file="MaximumConnectionsPerIpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin that limits the number of simultaneous active player sessions connected from the same IP address.
/// </summary>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Name), Description = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Description), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
[Guid("2C779F5E-379E-4CE0-BFCC-CA6455D757B3")]
public class MaximumConnectionsPerIpPlugIn : IPlayerStateChangingPlugIn, ISupportCustomConfiguration<MaximumConnectionsPerIpPlugInConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public MaximumConnectionsPerIpPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangingAsync(Player player, StateMachine.StateChangeEventArgs eventArgs)
    {
        if (eventArgs.NextState != PlayerState.Authenticated)
        {
            return;
        }

        var ipAddress = (player as IHasIpAddress)?.IpAddress;
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        var config = this.Configuration ?? CreateDefaultConfiguration();
        var players = await player.GameContext.GetPlayersAsync().ConfigureAwait(false);
        var sameIpCount = players.Count(p =>
            (p as IHasIpAddress)?.IpAddress == ipAddress
            && p != player
            && p.PlayerState.CurrentState != PlayerState.Initial
            && p.PlayerState.CurrentState != PlayerState.LoginScreen);

        if (sameIpCount >= config.MaximumConnectionsPerIp)
        {
            if (player.GameContext.FeaturePlugIns.GetPlugIn<ServerAuthoritativeAntiCheatPlugIn>() is { } antiCheat)
            {
                antiCheat.Report(
                    player,
                    new AntiCheatEvidence(
                        AntiCheatCategory.MultiClient,
                        config.RiskPoints,
                        "session.ip_connection_limit.exceeded",
                        $"ip={ipAddress};active={sameIpCount};limit={config.MaximumConnectionsPerIp}",
                        DateTimeOffset.UtcNow),
                    true);
            }

            player.Logger.LogWarning(
                "Login request for IP '{IpAddress}' exceeded the maximum connection limit of {Limit}. ObserveOnly={ObserveOnly}.",
                ipAddress,
                config.MaximumConnectionsPerIp,
                config.ObserveOnly);
            if (!ShouldReject(sameIpCount, config.MaximumConnectionsPerIp, config.ObserveOnly))
            {
                return;
            }

            player.LoginResultOverride = Views.Login.LoginResult.ServerIsFull;
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        return CreateDefaultConfiguration();
    }

    /// <summary>
    /// Determines whether an excessive connection must be rejected.
    /// </summary>
    internal static bool ShouldReject(int activeConnectionCount, int maximumConnections, bool observeOnly)
        => !observeOnly && activeConnectionCount >= Math.Max(1, maximumConnections);

    private static MaximumConnectionsPerIpPlugInConfiguration CreateDefaultConfiguration()
    {
        return new MaximumConnectionsPerIpPlugInConfiguration();
    }
}
