// <copyright file="AddServerAuthoritativeAntiCheatUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the central server-authoritative anti-cheat plugin to an existing Season 6 database.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6B85B0AE-CC4F-49E8-9F52-BE04EE4D06E1")]
public sealed class AddServerAuthoritativeAntiCheatUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Add server-authoritative anti-cheat";

    private const string PlugInDescription = "Adds the central risk engine and packet admission control in observe-only mode.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var typeId = typeof(ServerAuthoritativeAntiCheatPlugIn).GUID;
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeId) is { } existing)
        {
            existing.IsActive = true;
            if (string.IsNullOrWhiteSpace(existing.CustomConfiguration))
            {
                existing.SetConfiguration(new ServerAuthoritativeAntiCheatConfiguration(), null);
            }

            return ValueTask.CompletedTask;
        }

        var plugInConfiguration = context.CreateNew<PlugInConfiguration>();
        plugInConfiguration.SetGuid(typeId);
        plugInConfiguration.TypeId = typeId;
        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(new ServerAuthoritativeAntiCheatConfiguration(), null);
        gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        return ValueTask.CompletedTask;
    }
}
