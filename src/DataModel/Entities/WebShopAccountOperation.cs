// <copyright file="WebShopAccountOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// A server-authoritative account operation which is requested by an external webshop.
/// </summary>
/// <remarks>
/// The game server applies pending operations after it acquired the exclusive login reservation
/// and loaded the account. This prevents an external service from racing an in-memory account and
/// provides an exactly-once audit record through <see cref="Reference"/>.
/// </remarks>
[AggregateRoot]
public class WebShopAccountOperation
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the affected account.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public WebShopAccountOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the unique immutable reference of the webshop order.
    /// </summary>
    public string Reference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the first character name parameter.
    /// </summary>
    public string? CharacterName { get; set; }

    /// <summary>
    /// Gets or sets the second character name parameter, used by rename operations.
    /// </summary>
    public string? NewCharacterName { get; set; }

    /// <summary>
    /// Gets or sets the requested character class number.
    /// </summary>
    public byte? CharacterClassNumber { get; set; }

    /// <summary>
    /// Gets or sets the requested amount, for example the amount of vault money.
    /// </summary>
    public int? Amount { get; set; }

    /// <summary>
    /// Gets or sets the authenticated API client which requested the operation.
    /// </summary>
    public string? RequestedBy { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the time at which the game server applied the operation.
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Gets or sets the stable failure code, or <see langword="null"/> while pending or after success.
    /// </summary>
    public string? FailureCode { get; set; }
}
