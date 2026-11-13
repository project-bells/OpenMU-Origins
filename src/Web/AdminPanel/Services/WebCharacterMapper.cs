// <copyright file="WebCharacterMapper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Security.Cryptography;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shared helper methods for projecting persisted <see cref="Character"/> and <see cref="PlugInConfiguration"/>
/// state into web-facing DTOs.
/// </summary>
internal static class WebCharacterMapper
{
    /// <summary>
    /// Creates a reservation identifier accepted by <c>ILoginServer.TryReserveAccountAsync</c> (strictly greater than <see cref="byte.MaxValue"/>).
    /// </summary>
    /// <returns>A random positive reservation identifier.</returns>
    public static int CreateReservationId() => RandomNumberGenerator.GetInt32(byte.MaxValue + 1, int.MaxValue);

    /// <summary>
    /// Maps a numeric character class identifier to web display metadata.
    /// </summary>
    /// <param name="classNumber">The numeric character class identifier.</param>
    /// <returns>A tuple of normalized family id, full class name, and short label.</returns>
    public static (string ClassId, string ClassName, string ClassShort) GetClassInfo(int classNumber)
    {
        return classNumber switch
        {
            0 => ("dw", "Dark Wizard", "DW"),
            1 => ("dw", "Soul Master", "DW"),
            2 => ("dw", "Grand Master", "DW"),
            3 => ("dw", "Grand Master", "DW"),
            16 => ("dk", "Dark Knight", "DK"),
            17 => ("dk", "Blade Knight", "DK"),
            18 => ("dk", "Blade Master", "DK"),
            19 => ("dk", "Blade Master", "DK"),
            32 => ("elf", "Fairy Elf", "ELF"),
            33 => ("elf", "Muse Elf", "ELF"),
            34 => ("elf", "High Elf", "ELF"),
            35 => ("elf", "High Elf", "ELF"),
            48 => ("mg", "Magic Gladiator", "MG"),
            50 => ("mg", "Duel Master", "MG"),
            64 => ("dl", "Dark Lord", "DL"),
            66 => ("dl", "Lord Emperor", "DL"),
            80 => ("sum", "Summoner", "SUM"),
            81 => ("sum", "Bloody Summoner", "SUM"),
            82 => ("sum", "Dimension Master", "SUM"),
            83 => ("sum", "Dimension Master", "SUM"),
            96 => ("rf", "Rage Fighter", "RF"),
            98 => ("rf", "Fist Master", "RF"),
            _ => (classNumber >> 4) switch
            {
                0 => ("dw", "Dark Wizard", "DW"),
                1 => ("dk", "Dark Knight", "DK"),
                2 => ("elf", "Fairy Elf", "ELF"),
                3 => ("mg", "Magic Gladiator", "MG"),
                4 => ("dl", "Dark Lord", "DL"),
                5 => ("sum", "Summoner", "SUM"),
                6 => ("rf", "Rage Fighter", "RF"),
                _ => ("dk", "Blade Master", "DK"),
            },
        };
    }

    /// <summary>
    /// Maps a persisted <see cref="Character"/> into a <see cref="WebCharacterInfo"/> DTO.
    /// </summary>
    /// <param name="character">The persisted character.</param>
    /// <returns>The character summary DTO.</returns>
    public static WebCharacterInfo ToWebCharacterInfo(Character character)
    {
        var classNumber = character.CharacterClass?.Number ?? 0;
        var (classId, defaultClassName, classShort) = GetClassInfo(classNumber);
        string? rawClassName = character.CharacterClass?.Name;
        var className = string.IsNullOrWhiteSpace(rawClassName)
            ? defaultClassName
            : rawClassName;

        var level = Math.Max(1, (int)GetAttributeValue(character, Stats.Level, 1f));
        var masterLevel = Math.Max(0, (int)GetAttributeValue(character, Stats.MasterLevel, 0f));
        var resets = Math.Max(0, (int)GetAttributeValue(character, Stats.Resets, 0f));

        var strength = Math.Max(0, (int)GetAttributeValue(character, Stats.BaseStrength, 0f));
        var agility = Math.Max(0, (int)GetAttributeValue(character, Stats.BaseAgility, 0f));
        var vitality = Math.Max(0, (int)GetAttributeValue(character, Stats.BaseVitality, 0f));
        var energy = Math.Max(0, (int)GetAttributeValue(character, Stats.BaseEnergy, 0f));
        var leadership = Math.Max(0, (int)GetAttributeValue(character, Stats.BaseLeadership, 0f));

        var baseStrength = GetClassBaseStat(character.CharacterClass, Stats.BaseStrength);
        var baseAgility = GetClassBaseStat(character.CharacterClass, Stats.BaseAgility);
        var baseVitality = GetClassBaseStat(character.CharacterClass, Stats.BaseVitality);
        var baseEnergy = GetClassBaseStat(character.CharacterClass, Stats.BaseEnergy);
        var baseLeadership = GetClassBaseStat(character.CharacterClass, Stats.BaseLeadership);

        return new WebCharacterInfo(
            character.Id,
            character.Name,
            classNumber,
            classId,
            className,
            classShort,
            level,
            masterLevel,
            resets,
            character.LevelUpPoints,
            character.MasterLevelUpPoints,
            character.PlayerKillCount,
            character.State.ToString(),
            (int)character.State,
            character.StateRemainingSeconds,
            character.CharacterStatus.ToString(),
            character.Inventory?.Money ?? 0,
            character.CurrentMap?.Number,
            character.PositionX,
            character.PositionY,
            new WebCharacterStatsInfo(strength, agility, vitality, energy, leadership),
            new WebCharacterStatsInfo(baseStrength, baseAgility, baseVitality, baseEnergy, baseLeadership));
    }

    /// <summary>
    /// Finds a persisted attribute entry on a character matching the specified attribute definition.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <returns>The matching <see cref="StatAttribute"/> or <c>null</c>.</returns>
    public static StatAttribute? FindAttribute(Character character, AttributeDefinition definition)
    {
        return character.Attributes.FirstOrDefault(
            a => ReferenceEquals(a.Definition, definition) || a.Definition?.Id == definition.Id);
    }

    /// <summary>
    /// Reads the value of an attribute on a persisted character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <param name="fallbackValue">The default value if the attribute is missing.</param>
    /// <returns>The current value or <paramref name="fallbackValue"/>.</returns>
    public static float GetAttributeValue(Character character, AttributeDefinition definition, float fallbackValue)
    {
        return FindAttribute(character, definition)?.Value ?? fallbackValue;
    }

    /// <summary>
    /// Updates an attribute on a persisted character, creating the <see cref="StatAttribute"/> entry if missing.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The owning game configuration.</param>
    /// <param name="character">The character.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <param name="newValue">The new value.</param>
    public static void SetAttributeValue(
        IContext context,
        GameConfiguration gameConfiguration,
        Character character,
        AttributeDefinition definition,
        float newValue)
    {
        var existing = FindAttribute(character, definition);
        if (existing is not null)
        {
            existing.Value = newValue;
            return;
        }

        var persistentDefinition = gameConfiguration.Attributes.FirstOrDefault(a => a.Id == definition.Id) ?? definition;
        var created = context.CreateNew<StatAttribute>(persistentDefinition, newValue);
        character.Attributes.Add(created);
    }

    /// <summary>
    /// Resolves whether a plugin is enabled and extracts its custom configuration or defaults.
    /// </summary>
    /// <typeparam name="TPlugIn">The plugin type.</typeparam>
    /// <typeparam name="TConfig">The configuration type.</typeparam>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="gameConfigurationSource">The configuration data source for reference resolution.</param>
    /// <returns>A tuple indicating whether the plugin is active and its resolved configuration.</returns>
    public static (bool IsActive, TConfig Config) ResolvePlugInConfiguration<TPlugIn, TConfig>(
        GameConfiguration gameConfiguration,
        IDataSource<GameConfiguration> gameConfigurationSource)
        where TPlugIn : class
        where TConfig : class, new()
    {
        var plugInType = typeof(TPlugIn);
        var isDisabledByDefault = typeof(IDisabledByDefault).IsAssignableFrom(plugInType);
        var entry = gameConfiguration.PlugInConfigurations.FirstOrDefault(p => p.TypeId == plugInType.GUID);
        if (entry is null)
        {
            return (!isDisabledByDefault, new TConfig());
        }

        if (!entry.IsActive)
        {
            return (false, new TConfig());
        }

        var referenceHandler = new ByDataSourceReferenceHandler(gameConfigurationSource);
        var resolved = entry.GetConfiguration<TConfig>(referenceHandler) ?? new TConfig();
        return (true, resolved);
    }

    /// <summary>
    /// Gets the configured base stat value for a character class and attribute definition.
    /// </summary>
    /// <param name="characterClass">The character class.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <returns>The base value configured on the class, or zero.</returns>
    public static int GetClassBaseStat(CharacterClass? characterClass, AttributeDefinition definition)
    {
        var statDef = characterClass?.StatAttributes.FirstOrDefault(
            s => s.Attribute is not null && (ReferenceEquals(s.Attribute, definition) || s.Attribute.Id == definition.Id));
        return Math.Max(0, (int)(statDef?.BaseValue ?? 0f));
    }
}
