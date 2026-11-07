// <copyright file="CashShopProductInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// A configured product which can be selected by an authoritative item grant.
/// </summary>
public sealed record CashShopProductInfo(
    int ProductSequence,
    int PriceSequence,
    int Price,
    byte? ItemGroup,
    short? ItemNumber,
    byte ItemLevel,
    int Quantity,
    TimeSpan Duration);
