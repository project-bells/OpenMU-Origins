// <copyright file="CashShopCatalogController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Exposes the authoritative cash shop offer identifiers to trusted shop services.
/// </summary>
[ApiController]
[Route("api/cash-shop/catalog")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.CashShop)]
public sealed class CashShopCatalogController : ControllerBase
{
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopCatalogController"/> class.
    /// </summary>
    /// <param name="gameConfigurationSource">The authoritative game configuration source.</param>
    public CashShopCatalogController(IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._gameConfigurationSource = gameConfigurationSource;
    }

    /// <summary>
    /// Gets all enabled packages and products which can be selected by an item grant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative catalog.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var gameConfiguration = await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        if (gameConfiguration.CashShopConfiguration is not { } cashShop)
        {
            return this.Ok(Array.Empty<CashShopPackageInfo>());
        }

        var packages = cashShop.Packages
            .Where(package => package.IsForSale)
            .OrderBy(package => package.PackageSequence)
            .Select(package => new CashShopPackageInfo(
                package.PackageSequence,
                package.Price,
                package.CoinType,
                package.IsBundle,
                package.Products
                    .OrderBy(product => product.PriceSequence)
                    .Select(product => new CashShopProductInfo(
                        product.ProductSequence,
                        product.PriceSequence,
                        product.Price,
                        product.ItemDefinition?.Group,
                        product.ItemDefinition?.Number,
                        product.ItemLevel,
                        product.Quantity,
                        product.Duration))
                    .ToList()))
            .ToList();
        return this.Ok(packages);
    }
}
