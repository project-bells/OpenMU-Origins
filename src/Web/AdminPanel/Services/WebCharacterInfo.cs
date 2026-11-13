// <copyright file="WebCharacterInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Character state summary returned to the integrated website backend.
/// </summary>
/// <param name="Id">The character identifier.</param>
/// <param name="Name">The character name.</param>
/// <param name="ClassNumber">The numeric character class identifier.</param>
/// <param name="ClassId">The short normalized class family code.</param>
/// <param name="ClassName">The display name of the character class.</param>
/// <param name="ClassShort">The abbreviated class label.</param>
/// <param name="Level">The character level.</param>
/// <param name="MasterLevel">The character master level.</param>
/// <param name="Resets">The character reset count.</param>
/// <param name="LevelUpPoints">The remaining unspent stat points.</param>
/// <param name="MasterLevelUpPoints">The remaining master skill points.</param>
/// <param name="PlayerKillCount">The current player kill count.</param>
/// <param name="HeroState">The current hero/PK state name.</param>
/// <param name="HeroStateCode">The numeric hero/PK state value.</param>
/// <param name="StateRemainingSeconds">Remaining seconds for the active hero/PK state.</param>
/// <param name="CharacterStatus">The administrative character status.</param>
/// <param name="Money">The character inventory Zen.</param>
/// <param name="MapNumber">The current map number, if available.</param>
/// <param name="PositionX">The current X coordinate.</param>
/// <param name="PositionY">The current Y coordinate.</param>
/// <param name="Stats">The current base stats.</param>
/// <param name="BaseStats">The class minimum base stats for reset/refund calculation.</param>
public sealed record WebCharacterInfo(
    Guid Id,
    string Name,
    int ClassNumber,
    string ClassId,
    string ClassName,
    string ClassShort,
    int Level,
    int MasterLevel,
    int Resets,
    int LevelUpPoints,
    int MasterLevelUpPoints,
    int PlayerKillCount,
    string HeroState,
    int HeroStateCode,
    int StateRemainingSeconds,
    string CharacterStatus,
    int Money,
    short? MapNumber,
    byte PositionX,
    byte PositionY,
    WebCharacterStatsInfo Stats,
    WebCharacterStatsInfo BaseStats);

