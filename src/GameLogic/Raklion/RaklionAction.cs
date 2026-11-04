// <copyright file="RaklionAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Raklion;

/// <summary>
/// Actions of the Raklion event state machine.
/// </summary>
internal enum RaklionAction
{
    /// <summary>No transition is due.</summary>
    None,

    /// <summary>Starts a new event cycle.</summary>
    Start,

    /// <summary>Checks whether the spider eggs were destroyed.</summary>
    CheckSpiderEggs,

    /// <summary>Spawns Selupan after the appearance delay.</summary>
    SpawnSelupan,

    /// <summary>Closes the hatchery after the battle-entry period.</summary>
    CloseHatchery,

    /// <summary>Completes the battle successfully.</summary>
    EndBattleSuccess,

    /// <summary>Completes the battle as failed.</summary>
    EndBattleFailure,

    /// <summary>Removes the remaining battle players after the result period.</summary>
    RemovePlayers,

    /// <summary>Reopens the hatchery for the next cycle.</summary>
    Reopen,
}
