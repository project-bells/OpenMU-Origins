// <copyright file="AddVipGameServerUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the third, VIP-only Origins game server to an existing Season 6 configuration.
/// </summary>
[PlugIn]
[Display(Name = "Add Origins VIP game server")]
[Guid("00F028EA-17C2-4F9E-8146-330755EF5654")]
public sealed class AddVipGameServerUpdatePlugIn : UpdatePlugInBase
{
    private const byte VipServerId = 2;

    /// <inheritdoc />
    public override string Name => "Add Origins VIP game server";

    /// <inheritdoc />
    public override string Description => "Adds SV3-VIP (server ID 2, EXP 130x) to existing Origins Season 6 databases.";

    /// <inheritdoc />
    public override string DataInitializationKey => DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 7, 14, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var servers = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).ToList();
        if (servers.FirstOrDefault(server => server.ServerID == VipServerId) is { } existing)
        {
            existing.Description = "SV3-VIP";
            existing.ExperienceRate = 130.0f;
            existing.PvpEnabled = false;
            return;
        }

        var template = servers.OrderBy(server => server.ServerID).First();
        var vipServer = context.CreateNew<GameServerDefinition>();
        vipServer.SetGuid((short)VipServerId);
        vipServer.ServerID = VipServerId;
        vipServer.Description = "SV3-VIP";
        vipServer.ExperienceRate = 130.0f;
        vipServer.PvpEnabled = false;
        vipServer.GameConfiguration = gameConfiguration;
        vipServer.ServerConfiguration = template.ServerConfiguration;

        var nextPort = servers
            .SelectMany(server => server.Endpoints)
            .Select(endpoint => endpoint.NetworkPort)
            .DefaultIfEmpty(45900)
            .Max() + 1;
        var endpointIndex = 0;
        foreach (var client in await context.GetAsync<GameClientDefinition>().ConfigureAwait(false))
        {
            var endpoint = context.CreateNew<GameServerEndpoint>();
            endpoint.SetGuid((short)VipServerId, (short)endpointIndex++);
            endpoint.Client = client;
            endpoint.NetworkPort = nextPort++;
            vipServer.Endpoints.Add(endpoint);
        }
    }
}
