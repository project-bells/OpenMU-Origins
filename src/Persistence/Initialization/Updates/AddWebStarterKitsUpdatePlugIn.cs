// <copyright file="AddWebStarterKitsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the hidden, account-once starter kit products granted by the web portal.
/// </summary>
[PlugIn]
[Display(Name = "Add web starter kits", Description = "Adds seven class starter kits which expire seven days after extraction from Item Mall storage.")]
[Guid("14493976-770A-42FD-BA2A-75D0E133AAC4")]
public sealed class AddWebStarterKitsUpdatePlugIn : UpdatePlugInBase
{
    public const int FirstPackageSequence = 9001;
    public const int LastPackageSequence = 9007;

    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    public override string Name => "Add web starter kits";

    public override string Description => "Adds bound +9+16 class sets, a skilled weapon and Demon pet with a seven-day lifetime.";

    public override bool IsMandatory => true;

    public override bool RunsOnNewDatabase => true;

    public override DateTime CreatedAt => new(2026, 11, 22, 16, 0, 0, DateTimeKind.Utc);

    public override IEnumerable<UpdateDependency> DependsOn => [typeof(EnablePermanentCashShopProductsUpdatePlugIn).GUID];

    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CashShopConfiguration is not { } configuration)
        {
            return ValueTask.CompletedTask;
        }

        var placeholder = gameConfiguration.Items.First(item => item.Group == 13 && item.Number == 64); // Demon; special delivery creates the full kit.
        for (int index = 0; index < 7; index++)
        {
            var packageSequence = FirstPackageSequence + index;
            var productSequence = 2001 + index;
            var package = configuration.Packages.FirstOrDefault(candidate => candidate.PackageSequence == packageSequence);
            if (package is null)
            {
                package = context.CreateNew<CashShopPackage>();
                configuration.Packages.Add(package);
            }

            package.PackageSequence = packageSequence;
            // Hidden system-grant packages don't need a translated catalog caption; the client
            // displays the product metadata which ships with Main.exe.
            package.Name = new LocalizedString(string.Empty);
            package.Price = 0;
            package.CoinType = CashShopCoinType.WCoinC;
            package.IsForSale = false;
            package.IsGiftable = false;
            package.IsBundle = false;
            foreach (var obsolete in package.Products.ToList())
            {
                package.Products.Remove(obsolete);
            }

            var product = context.CreateNew<CashShopProduct>();
            product.ProductSequence = productSequence;
            product.PriceSequence = productSequence;
            product.Price = 0;
            product.ItemDefinition = placeholder;
            product.Quantity = 1;
            product.Duration = TimeSpan.FromDays(7);
            package.Products.Add(product);
        }

        return ValueTask.CompletedTask;
    }
}
