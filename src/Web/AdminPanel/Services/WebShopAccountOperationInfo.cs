// <copyright file="WebShopAccountOperationInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Describes a server-authoritative webshop account operation.
/// </summary>
public sealed record WebShopAccountOperationInfo(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] WebShopAccountOperationType OperationType,
    string Reference,
    string? CharacterName,
    string? NewCharacterName,
    byte? CharacterClassNumber,
    int? Amount,
    DateTime CreatedAt,
    DateTime? AppliedAt,
    string? FailureCode)
{
    /// <summary>
    /// Creates the API representation of an operation.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns>The API representation.</returns>
    public static WebShopAccountOperationInfo From(WebShopAccountOperation operation)
        => new(
            operation.GetId(),
            operation.OperationType,
            operation.Reference,
            operation.CharacterName,
            operation.NewCharacterName,
            operation.CharacterClassNumber,
            operation.Amount,
            operation.CreatedAt,
            operation.AppliedAt,
            operation.FailureCode);
}
