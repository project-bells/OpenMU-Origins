// <copyright file="WebCharacterOperationResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Result payload of an offline character self-service action.
/// </summary>
/// <param name="Status">The outcome status.</param>
/// <param name="Message">Human-readable outcome description.</param>
/// <param name="Character">Updated character state when successful or partially inspected.</param>
/// <param name="ZenSpent">Inventory Zen deducted by the operation.</param>
/// <param name="PointsDelta">Net change to unspent <c>LevelUpPoints</c> (positive on reset/refund, negative on add-stats).</param>
public sealed record WebCharacterOperationResult(
    WebCharacterOperationStatus Status,
    string Message,
    WebCharacterInfo? Character = null,
    int ZenSpent = 0,
    int PointsDelta = 0);

