// <copyright file="TripleRegularMonsterSpawnsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Triples regular field-monster quantities for an existing Season 6 database.
/// </summary>
[PlugIn]
[Display(Name = "Triple regular monster spawns", Description = "Triples normal automatic monster spawns without changing NPCs, event waves, or long-respawn bosses.")]
[Guid("73B5DB14-860D-4788-AB92-71D494739AE1")]
public sealed class TripleRegularMonsterSpawnsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Triple regular monster spawns";

    /// <inheritdoc />
    public override string Description => "Triples normal automatic monster quantities while leaving NPCs, event waves, and long-respawn bosses unchanged.";

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GameConfigurationInitializer.TripleRegularMonsterSpawns(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
