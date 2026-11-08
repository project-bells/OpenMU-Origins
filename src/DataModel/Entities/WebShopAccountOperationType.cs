// <copyright file="WebShopAccountOperationType.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The supported server-authoritative webshop account operations.
/// </summary>
public enum WebShopAccountOperationType
{
    /// <summary>
    /// Adds Zen to the account vault.
    /// </summary>
    AddVaultMoney = 0,

    /// <summary>
    /// Creates a level 400 character.
    /// </summary>
    CreateLevel400Character = 1,

    /// <summary>
    /// Renames an existing character.
    /// </summary>
    RenameCharacter = 2,
}
