// <copyright file="AddChaosCastleJewelDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the configured jewel drops which are distributed across Chaos Castle monster kills.
/// </summary>
[PlugIn]
[Guid("35BBB2EC-ED65-4205-BD8E-616BC030827C")]
public sealed class AddChaosCastleJewelDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Add Chaos Castle jewel drops";

    /// <inheritdoc />
    public override string Description => "Moves the fixed Chaos Castle monster jewel drops into editable map drop configuration.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 05, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
#pragma warning disable CS1998
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
#pragma warning restore CS1998
    {
        foreach (var definition in gameConfiguration.MiniGameDefinitions.Where(definition => definition.Type == MiniGameType.ChaosCastle))
        {
            ChaosCastleJewelDropConfiguration.Ensure(context, gameConfiguration, definition);
        }
    }
}
