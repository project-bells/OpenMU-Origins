// <copyright file="CharacterAddStatsRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Offline character stat allocation request submitted by the website BFF.
/// </summary>
public sealed class CharacterAddStatsRequest
{
    /// <summary>
    /// Gets or sets the strength points to add.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Strength { get; set; }

    /// <summary>
    /// Gets or sets the agility points to add.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Agility { get; set; }

    /// <summary>
    /// Gets or sets the vitality points to add.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Vitality { get; set; }

    /// <summary>
    /// Gets or sets the energy points to add.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Energy { get; set; }

    /// <summary>
    /// Gets or sets the leadership points to add (Dark Lord only).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Leadership { get; set; }
}

