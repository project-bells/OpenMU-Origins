// <copyright file="EnableMaximumConnectionsPerIpUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables observe-only multi-client detection for existing Season 6 databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("BE289A01-D457-43E1-A70F-993ED6E2AC18")]
public sealed class EnableMaximumConnectionsPerIpUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Enable multi-client detection";
    private const string PlugInDescription = "Enables observe-only detection of excessive connections from one IP address.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var typeId = typeof(MaximumConnectionsPerIpPlugIn).GUID;
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeId) is { } existing)
        {
            existing.IsActive = true;
            if (string.IsNullOrWhiteSpace(existing.CustomConfiguration))
            {
                existing.SetConfiguration(new MaximumConnectionsPerIpPlugInConfiguration(), null);
            }

            return ValueTask.CompletedTask;
        }

        var configuration = context.CreateNew<PlugInConfiguration>();
        configuration.SetGuid(typeId);
        configuration.TypeId = typeId;
        configuration.IsActive = true;
        configuration.SetConfiguration(new MaximumConnectionsPerIpPlugInConfiguration(), null);
        gameConfiguration.PlugInConfigurations.Add(configuration);
        return ValueTask.CompletedTask;
    }
}
