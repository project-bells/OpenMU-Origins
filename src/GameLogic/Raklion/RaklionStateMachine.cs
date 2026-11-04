// <copyright file="RaklionStateMachine.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Raklion;

/// <summary>
/// Selects the next action of the tick-driven Raklion event state machine.
/// </summary>
internal static class RaklionStateMachine
{
    /// <summary>
    /// Gets the action which is due for the current event state.
    /// </summary>
    /// <param name="state">The current event state.</param>
    /// <param name="elapsed">The time elapsed in the current state.</param>
    /// <param name="definition">The event timing definition.</param>
    /// <param name="isSelupanDead">Whether Selupan was defeated.</param>
    /// <param name="areBattlePlayersEmpty">Whether no battle players remain.</param>
    /// <param name="arePlayersRemoved">Whether the post-battle removal already ran.</param>
    /// <returns>The action which the context should execute.</returns>
    public static RaklionAction GetAction(
        RaklionState state,
        TimeSpan elapsed,
        RaklionEventDefinition definition,
        bool isSelupanDead,
        bool areBattlePlayersEmpty,
        bool arePlayersRemoved)
    {
        return state switch
        {
            RaklionState.End => RaklionAction.Start,
            RaklionState.Idle => RaklionAction.CheckSpiderEggs,
            RaklionState.Standby when elapsed >= definition.SelupanAppearanceDelay => RaklionAction.SpawnSelupan,
            RaklionState.StartBattle when isSelupanDead => RaklionAction.EndBattleSuccess,
            RaklionState.StartBattle when elapsed >= definition.HatcheryCloseDelay => RaklionAction.CloseHatchery,
            RaklionState.CloseDoor when isSelupanDead => RaklionAction.EndBattleSuccess,
            RaklionState.CloseDoor when areBattlePlayersEmpty => RaklionAction.EndBattleFailure,
            RaklionState.Notify4 when !arePlayersRemoved && elapsed >= definition.PlayerRemovalDelay => RaklionAction.RemovePlayers,
            RaklionState.Notify4 when elapsed >= definition.HatcheryOpenDelay => RaklionAction.Reopen,
            _ => RaklionAction.None,
        };
    }
}
