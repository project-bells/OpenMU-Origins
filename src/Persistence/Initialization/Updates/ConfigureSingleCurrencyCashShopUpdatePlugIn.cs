// <copyright file="ConfigureSingleCurrencyCashShopUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Converts the default Season 6 cash shop to the balanced eGis-MU WCoin (C)-only economy.
/// </summary>
[PlugIn]
[Display(Name = "Configure WCoin (C)-only cash shop", Description = "Uses only WCoin (C) and applies balanced prices for jewels, pets, lucky armor and Box of Kundun +1 through +5.")]
[Guid("B3842BEC-9DB0-4C71-82DF-EC6397A75E5A")]
public sealed class ConfigureSingleCurrencyCashShopUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Configure WCoin (C)-only cash shop";

    /// <inheritdoc />
    public override string Description => "Converts every cash-shop offer to WCoin (C) and applies the eGis-MU player-friendly price plan.";

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 15, 8, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(AddCashShopUpdatePlugIn).GUID];

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CashShopConfiguration is { } configuration)
        {
            CashShopInitializer.ApplySingleCurrencyEconomy(configuration);
        }

        return ValueTask.CompletedTask;
    }
}
