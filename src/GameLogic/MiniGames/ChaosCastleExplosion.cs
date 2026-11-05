// <copyright file="ChaosCastleExplosion.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Calculates the target position of an object which is pushed by a Chaos Castle monster explosion.
/// </summary>
internal static class ChaosCastleExplosion
{
    private static readonly (int Min, int Max)[] BlowOutDistance =
    {
        (3, 4),
        (3, 4),
        (2, 3),
        (0, 1),
    };

    /// <summary>
    /// Calculates a random target position away from the explosion, clamped to the map coordinate range.
    /// </summary>
    /// <param name="objectPosition">The current object position.</param>
    /// <param name="distance">The distance from the explosion.</param>
    /// <param name="directionX">The horizontal push direction.</param>
    /// <param name="directionY">The vertical push direction.</param>
    /// <param name="randomizer">The randomizer.</param>
    /// <returns>The target position.</returns>
    internal static Point GetTargetPosition(Point objectPosition, int distance, int directionX, int directionY, IRandomizer randomizer)
    {
        var (min, max) = BlowOutDistance[distance];
        var blowX = randomizer.NextInt(min, max + 1);
        var blowY = randomizer.NextInt(min, max + 1);
        if (randomizer.NextRandomBool())
        {
            if (blowX >= max)
            {
                blowX = max;
                blowY = min - randomizer.NextInt(0, 2);
            }
        }
        else if (blowY >= max)
        {
            blowY = max;
            blowX = min - randomizer.NextInt(0, 2);
        }

        blowX = Math.Max(0, blowX);
        blowY = Math.Max(0, blowY);
        var targetX = Math.Clamp(objectPosition.X + (blowX * directionX), byte.MinValue, byte.MaxValue);
        var targetY = Math.Clamp(objectPosition.Y + (blowY * directionY), byte.MinValue, byte.MaxValue);
        return new Point((byte)targetX, (byte)targetY);
    }

    /// <summary>
    /// Gets the direction away from an explosion, choosing randomly when both coordinates are equal.
    /// </summary>
    /// <param name="coordinate">The object coordinate.</param>
    /// <param name="explosionCoordinate">The explosion coordinate.</param>
    /// <param name="randomizer">The randomizer.</param>
    /// <returns><c>-1</c> or <c>1</c>.</returns>
    internal static int GetDirection(byte coordinate, byte explosionCoordinate, IRandomizer randomizer)
        => coordinate == explosionCoordinate
            ? randomizer.NextRandomBool() ? 1 : -1
            : Math.Sign(coordinate - explosionCoordinate);
}
