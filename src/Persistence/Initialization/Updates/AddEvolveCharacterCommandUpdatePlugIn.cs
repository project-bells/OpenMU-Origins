// <copyright file="AddEvolveCharacterCommandUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables the level 400 Class 3 evolution chat command in existing Season 6 databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A0BB26F4-72FD-43F1-B0D1-D062B030CE61")]
public sealed class AddEvolveCharacterCommandUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Add Class 3 evolution command";

    private const string PlugInDescription = "Enables /evo for level 400 second-generation characters.";

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
        var typeId = typeof(EvolveCharacterChatCommandPlugIn).GUID;
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeId) is not { } configuration)
        {
            configuration = context.CreateNew<PlugInConfiguration>();
            configuration.SetGuid(typeId);
            configuration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(configuration);
        }

        configuration.IsActive = true;
        return ValueTask.CompletedTask;
    }
}
