// <copyright file="CombatActionValidator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Contains pure server-side combat range validation rules.
/// </summary>
public static class CombatActionValidator
{
    /// <summary>
    /// Checks the tile distance without trusting any client-provided movement state.
    /// </summary>
    public static bool IsWithinRange(Point origin, Point target, int allowedRange)
        => Math.Max(Math.Abs(origin.X - target.X), Math.Abs(origin.Y - target.Y)) <= Math.Max(0, allowedRange);

    /// <summary>
    /// Checks whether a client-aimed area center is legal for the specified skill.
    /// Skills whose area is centered on the caster do not trust or validate the client point.
    /// </summary>
    public static bool IsAreaTargetWithinRange(Point casterPosition, Point targetAreaCenter, Skill skill, int tolerance)
    {
        bool usesClientTargetArea = skill.AreaSkillSettings is { UseTargetAreaFilter: true } or { EffectRange: > 0 };
        return !usesClientTargetArea || IsWithinRange(casterPosition, targetAreaCenter, skill.Range + Math.Max(0, tolerance));
    }
}
