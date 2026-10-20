// <copyright file="OriginsLostTowerPvpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Exempts player kills in Lost Tower on the Origins PVP server from the player-killer penalty.
/// </summary>
[PlugIn]
[Guid("43EF3C2E-6196-42A1-B91A-A83D82AD43EE")]
public sealed class OriginsLostTowerPvpPlugIn : IPlayerKillPenaltyExemptionPlugIn
{
    private const byte OriginsPvpServerId = 0;
    private const short LostTowerMapNumber = 4;

    /// <inheritdoc />
    public void CheckExemption(Player attacker, Player defender, PlayerKillPenaltyExemptionArgs eventArgs)
    {
        eventArgs.IsExempted |= attacker.GameContext is IGameServerContext { Id: OriginsPvpServerId }
                                  && attacker.CurrentMap?.Definition.Number == LostTowerMapNumber
                                  && defender.CurrentMap == attacker.CurrentMap;
    }
}
