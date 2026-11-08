// <copyright file="WebShopAccountOperationExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.WebShop;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Applies server-authoritative webshop operations while the game server owns the account session.
/// </summary>
public static class WebShopAccountOperationExtensions
{
    private const int PurchasedCharacterLevel = 400;

    /// <summary>
    /// Applies every pending webshop operation exactly once and persists the result with the account changes.
    /// </summary>
    /// <param name="player">The player whose account has just been loaded.</param>
    /// <returns>A value task which completes when all operations are persisted.</returns>
    public static async ValueTask ApplyPendingWebShopAccountOperationsAsync(this Player player)
    {
        if (player.Account is not { } account || player.GameContext.Configuration is not { } configuration)
        {
            return;
        }

        await player.RunPersistenceExclusiveAsync(async () =>
        {
            var operations = await player.PersistenceContext
                .GetPendingWebShopAccountOperationsAsync(account.GetId())
                .ConfigureAwait(false);
            if (operations.Count == 0)
            {
                return;
            }

            foreach (var operation in operations)
            {
                operation.FailureCode = operation.OperationType switch
                {
                    WebShopAccountOperationType.AddVaultMoney => ApplyVaultMoney(player, operation),
                    WebShopAccountOperationType.CreateLevel400Character => await CreateCharacterAsync(player, operation).ConfigureAwait(false),
                    WebShopAccountOperationType.RenameCharacter => await RenameCharacterAsync(player, operation).ConfigureAwait(false),
                    _ => "UNKNOWN_OPERATION",
                };
                if (operation.FailureCode is null)
                {
                    operation.AppliedAt = DateTime.UtcNow;
                }
            }

            await player.SaveProgressAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private static string? ApplyVaultMoney(Player player, WebShopAccountOperation operation)
    {
        if (operation.Amount is not > 0 || player.Account is not { } account)
        {
            return "INVALID_AMOUNT";
        }

        account.Vault ??= player.PersistenceContext.CreateNew<ItemStorage>();
        var configuredMaximum = player.GameContext.Configuration?.MaximumVaultMoney ?? 0;
        var maximum = configuredMaximum > 0 ? configuredMaximum : int.MaxValue;
        if ((long)account.Vault.Money + operation.Amount.Value > maximum)
        {
            return "VAULT_LIMIT_EXCEEDED";
        }

        account.Vault.Money += operation.Amount.Value;
        return null;
    }

    private static async ValueTask<string?> CreateCharacterAsync(Player player, WebShopAccountOperation operation)
    {
        var configuration = player.GameContext.Configuration;
        var account = player.Account!;
        if (operation.CharacterName is not { } name || !configuration.IsValidCharacterName(name))
        {
            return "INVALID_CHARACTER_NAME";
        }

        if (await player.PersistenceContext.GetAccountIdByCharacterNameAsync(name).ConfigureAwait(false) is not null)
        {
            return "CHARACTER_NAME_TAKEN";
        }

        if (account.GetFreeCharacterSlot(configuration) is not { } slot)
        {
            return "CHARACTER_SLOTS_FULL";
        }

        var characterClass = configuration.CharacterClasses.FirstOrDefault(characterClass =>
            characterClass.Number == operation.CharacterClassNumber && characterClass.IsMasterClass);
        if (characterClass?.HomeMap is null)
        {
            return "INVALID_CHARACTER_CLASS";
        }

        var character = player.PersistenceContext.CreateNew<Character>();
        character.Name = name;
        character.CharacterSlot = slot;
        player.PersistenceContext.InitializeNewCharacter(character, characterClass);
        character.Attributes.First(attribute => attribute.Definition == Stats.Level).Value = PurchasedCharacterLevel;
        character.Experience = CharacterProgression.CalculateNeededExperience(PurchasedCharacterLevel);
        var pointsPerLevel = characterClass.StatAttributes.First(attribute => attribute.Attribute == Stats.PointsPerLevelUp).BaseValue;
        character.LevelUpPoints += (int)((PurchasedCharacterLevel - 1) * pointsPerLevel);
        account.Characters.Add(character);
        player.GameContext.PlugInManager.GetPlugInPoint<ICharacterCreatedPlugIn>()?
            .CharacterCreated(account, character, player.PersistenceContext, configuration, player.Logger);
        return null;
    }

    private static async ValueTask<string?> RenameCharacterAsync(Player player, WebShopAccountOperation operation)
    {
        var configuration = player.GameContext.Configuration;
        if (operation.CharacterName is not { } oldName
            || operation.NewCharacterName is not { } newName
            || !configuration.IsValidCharacterName(newName))
        {
            return "INVALID_CHARACTER_NAME";
        }

        var character = player.Account!.Characters.FirstOrDefault(candidate => candidate.Name == oldName);
        if (character is null)
        {
            return "CHARACTER_NOT_FOUND";
        }

        if (await player.PersistenceContext.GetAccountIdByCharacterNameAsync(newName).ConfigureAwait(false) is not null)
        {
            return "CHARACTER_NAME_TAKEN";
        }

        character.Name = newName;
        return null;
    }
}
