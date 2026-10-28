// <copyright file="WebShopPackage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.WebShop;

/// <summary>
/// Defines a WCoin package sold for real currency.
/// </summary>
public sealed class WebShopPackage
{
    /// <summary>
    /// Gets or sets the stable package code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price in Thai baht.
    /// </summary>
    public decimal PriceThb { get; set; }

    /// <summary>
    /// Gets or sets the amount of WCoinC.
    /// </summary>
    public int WCoinC { get; set; }
}
