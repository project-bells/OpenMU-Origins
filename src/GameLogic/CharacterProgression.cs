// <copyright file="CharacterProgression.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Contains shared calculations for persisted character progression.
/// </summary>
public static class CharacterProgression
{
    /// <summary>
    /// Calculates the accumulated experience at the specified character level.
    /// </summary>
    /// <param name="level">The character level.</param>
    /// <returns>The accumulated experience.</returns>
    public static long CalculateNeededExperience(long level)
    {
        if (level == 0)
        {
            return 0;
        }

        if (level < 256)
        {
            return 10 * (level + 8) * (level - 1) * (level - 1);
        }

        return (10 * (level + 8) * (level - 1) * (level - 1))
               + (1000 * (level - 247) * (level - 256) * (level - 256));
    }
}
