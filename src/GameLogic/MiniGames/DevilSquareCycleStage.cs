// <copyright file="DevilSquareCycleStage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Stages of one Devil Square event cycle.
/// </summary>
internal enum DevilSquareCycleStage
{
    /// <summary>The entrance is open and the battle has not started.</summary>
    Waiting,

    /// <summary>The battle is running.</summary>
    Playing,

    /// <summary>The battle ended with at least one finisher eligible for ranking and rewards.</summary>
    CompletedWithFinishers,

    /// <summary>The battle ended without a finisher.</summary>
    CompletedEmpty,
}
