// <copyright file="IllusionTempleUpdateResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>
/// Localized resources for <see cref="DisableIncompleteIllusionTempleUpdatePlugIn"/>.
/// </summary>
public static class IllusionTempleUpdateResources
{
    /// <summary>
    /// Gets the localized update name.
    /// </summary>
    public static string Name => PlugInResources.ResourceManager.GetString("DisableIncompleteIllusionTempleUpdatePlugIn_Name")!;

    /// <summary>
    /// Gets the localized update description.
    /// </summary>
    public static string Description => PlugInResources.ResourceManager.GetString("DisableIncompleteIllusionTempleUpdatePlugIn_Description")!;
}
