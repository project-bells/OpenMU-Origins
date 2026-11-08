// <copyright file="EnableMasterExperienceOnAllMonstersUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Allows master characters to gain master experience from monsters of every level.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("77810CFD-17E9-4841-A36F-80ED4244FC34")]
public sealed class EnableMasterExperienceOnAllMonstersUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Enable master experience on all monsters";
    private const string PlugInDescription = "Removes the monster-level gate which prevented master experience in areas such as Kanturu.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 8, 12, 5, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        gameConfiguration.MinimumMonsterLevelForMasterExperience = 0;
        return ValueTask.CompletedTask;
    }
}
