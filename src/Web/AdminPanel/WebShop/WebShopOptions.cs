// <copyright file="WebShopOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.WebShop;

/// <summary>
/// Defines the player-facing web shop and its payment packages.
/// </summary>
public sealed class WebShopOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "WebShop";

    /// <summary>
    /// Gets or sets a value indicating whether the shop is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the PayPal REST API client identifier.
    /// </summary>
    public string PayPalClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PayPal REST API secret.
    /// </summary>
    public string PayPalClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the PayPal sandbox is used.
    /// </summary>
    public bool UsePayPalSandbox { get; set; } = true;

    /// <summary>
    /// Gets or sets the packages which can be bought.
    /// </summary>
    public IList<WebShopPackage> Packages { get; set; } = new List<WebShopPackage>();
}
