// <copyright file="BloodCastleCycleStage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Stages of a Blood Castle event cycle.
/// </summary>
internal enum BloodCastleCycleStage
{
    /// <summary>The players are fighting toward the castle gate.</summary>
    Monsters,

    /// <summary>The castle gate was destroyed.</summary>
    GateDestroyed,

    /// <summary>The statue was destroyed and its quest item became available.</summary>
    StatueDestroyed,

    /// <summary>The quest item was returned successfully.</summary>
    CompletedSuccessfully,

    /// <summary>The event ended without a successful quest hand-in.</summary>
    TimedOut,
}
