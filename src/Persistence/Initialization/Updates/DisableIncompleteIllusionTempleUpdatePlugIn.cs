// <copyright file="DisableIncompleteIllusionTempleUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Disables ticket acquisition for Illusion Temple until its runtime state machine is implemented.
/// </summary>
[PlugIn]
[Display(
    Name = nameof(IllusionTempleUpdateResources.Name),
    Description = nameof(IllusionTempleUpdateResources.Description),
    ResourceType = typeof(IllusionTempleUpdateResources))]
[Guid("8895F429-C9E1-4DFC-9FF0-63DA4F2608B8")]
public sealed class DisableIncompleteIllusionTempleUpdatePlugIn : UpdatePlugInBase
{
    private const int TicketCraftingNumber = 37;
    private static readonly int[] TicketPackageSequences = [104, 105];

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => IllusionTempleUpdateResources.Name;

    /// <inheritdoc />
    public override string Description => IllusionTempleUpdateResources.Description;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Monsters.FirstOrDefault(monster => monster.NpcWindow == NpcWindow.ChaosMachine) is { } chaosGoblin)
        {
            foreach (var crafting in chaosGoblin.ItemCraftings.Where(crafting => crafting.Number == TicketCraftingNumber).ToList())
            {
                chaosGoblin.ItemCraftings.Remove(crafting);
            }
        }

        if (gameConfiguration.CashShopConfiguration is { } cashShop)
        {
            foreach (var package in cashShop.Packages.Where(package => TicketPackageSequences.Contains(package.PackageSequence)))
            {
                package.IsForSale = false;
                package.IsGiftable = false;
            }
        }

        return ValueTask.CompletedTask;
    }
}
