// <copyright file="WebCharacterStatsInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Character base attribute values exposed to player self-service APIs.
/// </summary>
/// <param name="Strength">The base strength.</param>
/// <param name="Agility">The base agility.</param>
/// <param name="Vitality">The base vitality.</param>
/// <param name="Energy">The base energy.</param>
/// <param name="Leadership">The base leadership (command).</param>
public sealed record WebCharacterStatsInfo(
    int Strength,
    int Agility,
    int Vitality,
    int Energy,
    int Leadership);

