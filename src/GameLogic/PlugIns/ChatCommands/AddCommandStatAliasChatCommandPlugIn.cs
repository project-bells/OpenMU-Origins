// <copyright file="AddCommandStatAliasChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the commonly used <c>/addcom</c> alias for adding leadership points.
/// </summary>
[Guid("61499BBD-183C-4519-96F6-FF88E429E8A5")]
[PlugIn]
[Display(Name = nameof(PlugInResources.AddCommandStatChatCommandPlugIn_Name), Description = nameof(PlugInResources.AddCommandStatChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(AddSingleStatChatCommandArgs), MinimumStatus)]
public class AddCommandStatAliasChatCommandPlugIn : AddStatChatCommandPlugIn, IDisabledByDefault
{
    private const string Command = "/addcom";

    private const CharacterStatus MinimumStatus = CharacterStatus.Normal;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    public override async ValueTask HandleCommandAsync(Player player, string command)
    {
        command = "/add cmd" + command[Command.Length..];
        await base.HandleCommandAsync(player, command).ConfigureAwait(false);
    }
}
