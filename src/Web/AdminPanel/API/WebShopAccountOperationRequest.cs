// <copyright file="WebShopAccountOperationRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Requests a server-authoritative account operation for an external webshop order.
/// </summary>
public sealed class WebShopAccountOperationRequest
{
    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WebShopAccountOperationType? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the immutable external order reference.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets the existing or new character name.
    /// </summary>
    [StringLength(10, MinimumLength = 2)]
    public string? CharacterName { get; set; }

    /// <summary>
    /// Gets or sets the new name for a rename operation.
    /// </summary>
    [StringLength(10, MinimumLength = 2)]
    public string? NewCharacterName { get; set; }

    /// <summary>
    /// Gets or sets the class number for a character creation operation.
    /// </summary>
    public byte? CharacterClassNumber { get; set; }

    /// <summary>
    /// Gets or sets the amount for a vault-money operation.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? Amount { get; set; }
}
