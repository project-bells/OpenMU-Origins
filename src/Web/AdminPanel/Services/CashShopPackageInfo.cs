// <copyright file="CashShopPackageInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// An enabled package in the authoritative cash shop catalog.
/// </summary>
public sealed record CashShopPackageInfo(
    int PackageSequence,
    int Price,
    CashShopCoinType CoinType,
    bool IsBundle,
    IReadOnlyList<CashShopProductInfo> Products);
