// <copyright file="ChaosCastleCycleStage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Stages of one Chaos Castle event cycle.
/// </summary>
internal enum ChaosCastleCycleStage
{
    /// <summary>The entrance is open and the battle has not started.</summary>
    Waiting,

    /// <summary>The battle is running.</summary>
    Playing,

    /// <summary>The battle ended with at least one finisher and a winner.</summary>
    CompletedWithWinner,

    /// <summary>The battle ended without a surviving player.</summary>
    CompletedEmpty,
}
