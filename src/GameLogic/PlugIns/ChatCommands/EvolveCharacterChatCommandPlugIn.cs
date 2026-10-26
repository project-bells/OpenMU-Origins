// <copyright file="EvolveCharacterChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Evolves a level 400 second-generation character to its third/master class.
/// </summary>
[Guid("B4A35B97-F4BF-4EB8-A0C2-A759BF3F68BB")]
[PlugIn]
[Display(Name = "Class 3 evolution command", Description = "Allows a level 400 character to change to its third class with /evo.")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public sealed class EvolveCharacterChatCommandPlugIn : IChatCommandPlugIn
{
    internal const int RequiredLevel = 400;

    private const string Command = "/evo";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.SelectedCharacter is not { } character || player.Attributes is not { } attributes)
        {
            return;
        }

        int level = (int)attributes[Stats.Level];
        if (level != RequiredLevel)
        {
            await player.ShowBlueMessageAsync($"You must be level {RequiredLevel} to use /evo.").ConfigureAwait(false);
            return;
        }

        if (!CanEvolve(character.CharacterClass, level))
        {
            await player.ShowBlueMessageAsync("Your current class cannot evolve to Class 3.").ConfigureAwait(false);
            return;
        }

        character.CharacterClass = character.CharacterClass!.NextGenerationClass!;
        await player.ForEachWorldObserverAsync<ILegacyQuestRewardPlugIn>(
            plugIn => plugIn.ShowAsync(
                player,
                QuestRewardType.CharacterEvolutionSecondToThird,
                0,
                null),
            true).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateMasterStatsPlugIn>(plugIn => plugIn.SendMasterStatsAsync()).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
        await player.ShowBlueMessageAsync("Class 3 evolution completed successfully.").ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether the current class has a third/master-class successor and the required level.
    /// </summary>
    /// <param name="characterClass">The current character class.</param>
    /// <param name="level">The server-side character level.</param>
    /// <returns><c>true</c> if the character may evolve; otherwise, <c>false</c>.</returns>
    internal static bool CanEvolve(CharacterClass? characterClass, int level)
        => level == RequiredLevel && characterClass?.NextGenerationClass is { IsMasterClass: true };
}
