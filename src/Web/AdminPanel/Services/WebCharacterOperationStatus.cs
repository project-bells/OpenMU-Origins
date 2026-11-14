// <copyright file="WebCharacterOperationStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Result status for offline web character self-service operations.
/// </summary>
public enum WebCharacterOperationStatus
{
    /// <summary>
    /// The operation succeeded and changes were persisted.
    /// </summary>
    Success,

    /// <summary>
    /// The target account does not exist.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// The target character does not belong to the account or does not exist.
    /// </summary>
    CharacterNotFound,

    /// <summary>
    /// The account is currently logged in; player must log out first.
    /// </summary>
    AccountOnline,

    /// <summary>
    /// The account is banned or temporarily banned.
    /// </summary>
    AccountBanned,

    /// <summary>
    /// The requested feature is disabled by server configuration.
    /// </summary>
    FeatureDisabled,

    /// <summary>
    /// The character does not meet the required level.
    /// </summary>
    InsufficientLevel,

    /// <summary>
    /// The character does not have enough inventory Zen.
    /// </summary>
    InsufficientZen,

    /// <summary>
    /// The character does not have the required reset items.
    /// </summary>
    InsufficientItems,

    /// <summary>
    /// The character has already reached the configured reset limit.
    /// </summary>
    ResetLimitReached,

    /// <summary>
    /// The character has no PK status or kill count to clear.
    /// </summary>
    NotPlayerKiller,

    /// <summary>
    /// The character does not have enough unspent stat points.
    /// </summary>
    InsufficientPoints,

    /// <summary>
    /// The request arguments were invalid or disallowed for the character's class.
    /// </summary>
    InvalidInput,

    /// <summary>
    /// The character has no invested stat points to refund.
    /// </summary>
    NoStatsToReset,
}

