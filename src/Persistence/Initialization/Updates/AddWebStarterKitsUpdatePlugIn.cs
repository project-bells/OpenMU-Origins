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
    public const int FirstQuestPackageSequence = 9101;
    public const int LastQuestPackageSequence = 9110;

    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    public override string Name => "Add web starter kits";

    public override string Description => "Adds bound starter kits and the class-change quest items sold for 20 WCoin (C).";

    public override bool IsMandatory => true;

    public override bool RunsOnNewDatabase => true;

    public override DateTime CreatedAt => new(2026, 11, 22, 16, 0, 0, DateTimeKind.Utc);

    public override int Version => 2;

    public override DateTime UpdatedAt => new(2026, 11, 22, 18, 30, 0, DateTimeKind.Utc);

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

        (short Number, byte Level)[] questItems =
        [
            (23, 0), // Scroll of Emperor
            (23, 1), // Ring of Honor
            (24, 0), // Broken Sword
            (24, 1), // Dark Stone
            (25, 0), // Tear of Elf
            (26, 0), // Soul Shard of Wizard
            (65, 0), // Flame of Death Beam Knight
            (66, 0), // Horn of Hell Maine
            (67, 0), // Feather of Dark Phoenix
            (68, 0), // Eye of Abyssal
        ];

        for (int index = 0; index < questItems.Length; index++)
        {
            var packageSequence = FirstQuestPackageSequence + index;
            var productSequence = 2101 + index;
            var (itemNumber, itemLevel) = questItems[index];
            var itemDefinition = gameConfiguration.Items.Single(item => item.Group == 14 && item.Number == itemNumber);
            var package = configuration.Packages.FirstOrDefault(candidate => candidate.PackageSequence == packageSequence);
            if (package is null)
            {
                package = context.CreateNew<CashShopPackage>();
                configuration.Packages.Add(package);
            }

            package.PackageSequence = packageSequence;
            package.Name = new LocalizedString(itemDefinition.Name.ValueInNeutralLanguage);
            package.Price = 20;
            package.CoinType = CashShopCoinType.WCoinC;
            package.IsForSale = true;
            package.IsGiftable = false;
            package.IsBundle = false;
            foreach (var obsolete in package.Products.ToList())
            {
                package.Products.Remove(obsolete);
            }

            var product = context.CreateNew<CashShopProduct>();
            product.ProductSequence = productSequence;
            product.PriceSequence = productSequence;
            product.Price = 20;
            product.ItemDefinition = itemDefinition;
            product.ItemLevel = itemLevel;
            product.Quantity = 1;
            product.Duration = TimeSpan.Zero;
            package.Products.Add(product);
        }

        return ValueTask.CompletedTask;
    }
}
