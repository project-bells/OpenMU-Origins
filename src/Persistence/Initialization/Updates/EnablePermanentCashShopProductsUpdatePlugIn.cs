// <copyright file="EnablePermanentCashShopProductsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes the stock client's time-limited offers permanent and enables packages
/// whose item definitions are complete, so every visible sale can be delivered.
/// </summary>
[PlugIn]
[Display(Name = "Enable permanent cash-shop products", Description = "Makes 1/7/30-day cash-shop products permanent and enables every fully deliverable package.")]
[Guid("E6B8DC77-527B-4EE4-9905-53371D27D54B")]
public sealed class EnablePermanentCashShopProductsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Enable permanent cash-shop products";

    /// <inheritdoc />
    public override string Description => "Converts duration-based offers to permanent items and enables packages with complete item definitions.";

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 17, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn =>
    [
        typeof(ConfigureSingleCurrencyCashShopUpdatePlugIn).GUID,
        typeof(AddLuckyItemsUpdatePlugIn).GUID,
    ];

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CashShopConfiguration is not { } configuration)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var package in configuration.Packages)
        {
            foreach (var product in package.Products)
            {
                product.Duration = TimeSpan.Zero;
            }

            package.IsForSale = package.Products.Count > 0
                                && package.Products.All(product => product.ItemDefinition is not null);
        }

        return ValueTask.CompletedTask;
    }
}
