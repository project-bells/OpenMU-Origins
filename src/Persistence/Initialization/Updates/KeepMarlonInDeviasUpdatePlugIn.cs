// <copyright file="KeepMarlonInDeviasUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Keeps Marlon at a fixed position in Devias and removes his former wandering spawn points.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E6B69CE7-6997-4C69-B26F-AEDE8C23F54B")]
public sealed class KeepMarlonInDeviasUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Keep Marlon in Devias";
    private const string PlugInDescription = "Removes Marlon's wandering spawn points and keeps him at position 197, 48 in Devias.";
    private const short MarlonNumber = 229;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 28, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var marlon = gameConfiguration.Monsters.Single(monster => monster.Number == MarlonNumber);
        var devias = gameConfiguration.Maps.Single(map => map.Number == Devias.Number);
        var marlonSpawns = gameConfiguration.Maps
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn.MonsterDefinition?.Number == MarlonNumber)
            .ToList();

        var deviasSpawn = marlonSpawns.FirstOrDefault(spawn => spawn.GameMap?.Number == Devias.Number);
        if (deviasSpawn is null)
        {
            deviasSpawn = context.CreateNew<MonsterSpawnArea>();
            deviasSpawn.SetGuid(39);
            deviasSpawn.GameMap = devias;
            deviasSpawn.MonsterDefinition = marlon;
            devias.MonsterSpawns.Add(deviasSpawn);
        }

        deviasSpawn.SpawnTrigger = SpawnTrigger.Automatic;
        deviasSpawn.Direction = Direction.SouthEast;
        deviasSpawn.X1 = 197;
        deviasSpawn.X2 = 197;
        deviasSpawn.Y1 = 48;
        deviasSpawn.Y2 = 48;
        deviasSpawn.Quantity = 1;

        foreach (var obsoleteSpawn in marlonSpawns.Where(spawn => spawn != deviasSpawn))
        {
            obsoleteSpawn.GameMap?.MonsterSpawns.Remove(obsoleteSpawn);
            obsoleteSpawn.GameMap = null;
            await context.DeleteAsync(obsoleteSpawn).ConfigureAwait(false);
        }
    }
}
