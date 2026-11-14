// <copyright file="WebCharacterSelfService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Threading;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Executes offline-locked, server-authoritative character self-service actions
/// (character reset, PK clear, stat allocation, and stat reset) on behalf of the website BFF.
/// </summary>
public sealed class WebCharacterSelfService
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;
    private readonly ILoginServer _loginServer;
    private readonly ILogger<WebCharacterSelfService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebCharacterSelfService"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration data source.</param>
    /// <param name="loginServer">The login server used for offline account reservation.</param>
    /// <param name="logger">The logger.</param>
    public WebCharacterSelfService(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource,
        ILoginServer loginServer,
        ILogger<WebCharacterSelfService> logger)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
        this._loginServer = loginServer;
        this._logger = logger;
    }

    /// <summary>
    /// Performs a character reset using the server's <see cref="ResetConfiguration"/> rules while offline.
    /// </summary>
    /// <param name="loginName">The owning account's login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public ValueTask<WebCharacterOperationResult> ResetCharacterAsync(
        string loginName,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        return this.ExecuteOfflineCharacterMutationAsync(
            loginName,
            characterName,
            (context, gameConfig, character) => this.ApplyResetAsync(context, gameConfig, character, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Clears a character's PK count and hero state using <see cref="PkClearChatCommandPlugIn.PKClearConfiguration"/> rules while offline.
    /// </summary>
    /// <param name="loginName">The owning account's login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public ValueTask<WebCharacterOperationResult> ClearPkAsync(
        string loginName,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        return this.ExecuteOfflineCharacterMutationAsync(
            loginName,
            characterName,
            (_, gameConfig, character) => ValueTask.FromResult(this.ApplyPkClear(gameConfig, character)),
            cancellationToken);
    }

    /// <summary>
    /// Allocates unspent <see cref="Character.LevelUpPoints"/> to base character stats while offline.
    /// </summary>
    /// <param name="loginName">The owning account's login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="strength">Points to add to strength.</param>
    /// <param name="agility">Points to add to agility.</param>
    /// <param name="vitality">Points to add to vitality.</param>
    /// <param name="energy">Points to add to energy.</param>
    /// <param name="leadership">Points to add to leadership.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public ValueTask<WebCharacterOperationResult> AddStatsAsync(
        string loginName,
        string characterName,
        int strength,
        int agility,
        int vitality,
        int energy,
        int leadership,
        CancellationToken cancellationToken = default)
    {
        return this.ExecuteOfflineCharacterMutationAsync(
            loginName,
            characterName,
            (context, gameConfig, character) => ValueTask.FromResult(ApplyAddStats(
                context,
                gameConfig,
                character,
                strength,
                agility,
                vitality,
                energy,
                leadership)),
            cancellationToken);
    }

    /// <summary>
    /// Refunds invested character stats back to base class values using <see cref="StatResetConfiguration"/> while offline.
    /// </summary>
    /// <param name="loginName">The owning account's login name.</param>
    /// <param name="characterName">The character name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public ValueTask<WebCharacterOperationResult> ResetStatsAsync(
        string loginName,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        return this.ExecuteOfflineCharacterMutationAsync(
            loginName,
            characterName,
            (context, gameConfig, character) => this.ApplyStatResetAsync(context, gameConfig, character, cancellationToken),
            cancellationToken);
    }

    private static (bool ShouldSave, WebCharacterOperationResult Result) ApplyAddStats(
        IContext context,
        GameConfiguration gameConfig,
        Character character,
        int strength,
        int agility,
        int vitality,
        int energy,
        int leadership)
    {
        if (strength < 0 || agility < 0 || vitality < 0 || energy < 0 || leadership < 0)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.InvalidInput, "Stat increments cannot be negative."));
        }

        long totalRequestedLong = (long)strength + agility + vitality + energy + leadership;
        if (totalRequestedLong <= 0 || totalRequestedLong > int.MaxValue)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.InvalidInput, "At least one positive stat increment is required."));
        }

        var totalRequested = (int)totalRequestedLong;
        if (character.LevelUpPoints < totalRequested)
        {
            return (false, new WebCharacterOperationResult(
                WebCharacterOperationStatus.InsufficientPoints,
                $"Insufficient LevelUpPoints ({character.LevelUpPoints} available, {totalRequested} requested)."));
        }

        var allocations = new (AttributeDefinition Definition, int Amount)[]
        {
            (Stats.BaseStrength, strength),
            (Stats.BaseAgility, agility),
            (Stats.BaseVitality, vitality),
            (Stats.BaseEnergy, energy),
            (Stats.BaseLeadership, leadership),
        };

        var classStats = character.CharacterClass?.StatAttributes;
        foreach (var (definition, amount) in allocations)
        {
            if (amount <= 0)
            {
                continue;
            }

            if (classStats is not null)
            {
                var statDef = classStats.FirstOrDefault(
                    s => s.Attribute is not null && (ReferenceEquals(s.Attribute, definition) || s.Attribute.Id == definition.Id));
                if (statDef is null || !statDef.IncreasableByPlayer)
                {
                    return (false, new WebCharacterOperationResult(
                        WebCharacterOperationStatus.InvalidInput,
                        $"Stat '{definition.Designation}' cannot be increased for class '{character.CharacterClass?.Name}'."));
                }
            }
        }

        foreach (var (definition, amount) in allocations)
        {
            if (amount <= 0)
            {
                continue;
            }

            var current = WebCharacterMapper.GetAttributeValue(character, definition, 0f);
            WebCharacterMapper.SetAttributeValue(context, gameConfig, character, definition, current + amount);
        }

        character.LevelUpPoints -= totalRequested;
        return (true, new WebCharacterOperationResult(
            WebCharacterOperationStatus.Success,
            $"Added {totalRequested} stat points.",
            WebCharacterMapper.ToWebCharacterInfo(character),
            0,
            -totalRequested));
    }

    private static async ValueTask<WebCharacterOperationResult?> TryConsumeZenAndItemsAsync(
        IContext context,
        Character character,
        int requiredZen,
        ItemDefinition? requiredItemDefinition,
        int requiredItemAmount,
        CancellationToken cancellationToken)
    {
        var itemsToConsume = new List<Item>();
        if (requiredItemAmount > 0 && requiredItemDefinition is not null)
        {
            var matched = character.Inventory?.Items
                .Where(item => item.Definition is { } def
                               && def.Group == requiredItemDefinition.Group
                               && def.Number == requiredItemDefinition.Number)
                .Take(requiredItemAmount)
                .ToList() ?? [];
            if (matched.Count < requiredItemAmount)
            {
                return new WebCharacterOperationResult(
                    WebCharacterOperationStatus.InsufficientItems,
                    $"Requires {requiredItemAmount}x {requiredItemDefinition.Name}.");
            }

            itemsToConsume = matched;
        }

        var inventory = character.Inventory;
        var currentMoney = inventory?.Money ?? 0;
        if (requiredZen > 0 && (inventory is null || currentMoney < requiredZen))
        {
            return new WebCharacterOperationResult(
                WebCharacterOperationStatus.InsufficientZen,
                $"Insufficient Zen (requires {requiredZen:N0}, current {currentMoney:N0}).");
        }

        if (requiredZen > 0 && inventory is not null)
        {
            inventory.Money -= requiredZen;
        }

        foreach (var item in itemsToConsume)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inventory!.Items.Remove(item);
            await context.DeleteAsync(item).ConfigureAwait(false);
        }

        return null;
    }

    private static void MoveCharacterToHomeMap(Character character)
    {
        var homeMapDef = character.CharacterClass?.HomeMap;
        if (homeMapDef is null)
        {
            return;
        }

        character.CurrentMap = homeMapDef;
        var spawnGate = homeMapDef.ExitGates.FirstOrDefault(g => g.IsSpawnGate);
        if (spawnGate is not null)
        {
            var minX = Math.Min(spawnGate.X1, spawnGate.X2);
            var maxX = Math.Max(spawnGate.X1, spawnGate.X2);
            var minY = Math.Min(spawnGate.Y1, spawnGate.Y2);
            var maxY = Math.Max(spawnGate.Y1, spawnGate.Y2);
            character.PositionX = (byte)Random.Shared.Next(minX, maxX + 1);
            character.PositionY = (byte)Random.Shared.Next(minY, maxY + 1);
        }
    }

    private async ValueTask<(bool ShouldSave, WebCharacterOperationResult Result)> ApplyResetAsync(
        IContext context,
        GameConfiguration gameConfig,
        Character character,
        CancellationToken cancellationToken)
    {
        var (isActive, configuration) = WebCharacterMapper.ResolvePlugInConfiguration<ResetFeaturePlugIn, ResetConfiguration>(
            gameConfig,
            this._gameConfigurationSource);
        if (!isActive)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.FeatureDisabled, "Character reset system is not enabled on this server."));
        }

        var level = (int)WebCharacterMapper.GetAttributeValue(character, Stats.Level, 1f);
        if (level < configuration.RequiredLevel)
        {
            return (false, new WebCharacterOperationResult(
                WebCharacterOperationStatus.InsufficientLevel,
                $"Character level {level} is below required level {configuration.RequiredLevel}."));
        }

        var currentResetCount = Math.Max(0, (int)WebCharacterMapper.GetAttributeValue(character, Stats.Resets, 0f));
        if (configuration.ResetLimit > 0 && currentResetCount >= configuration.ResetLimit)
        {
            return (false, new WebCharacterOperationResult(
                WebCharacterOperationStatus.ResetLimitReached,
                $"Character has reached the reset limit ({configuration.ResetLimit})."));
        }

        var defaultPointsOverride = WebCharacterMapper.GetClassBaseStat(character.CharacterClass, Stats.PointsPerReset);
        var pointsOverride = (int)WebCharacterMapper.GetAttributeValue(character, Stats.PointsPerReset, defaultPointsOverride);
        var progression = ResetProgressionCalculator.Calculate(currentResetCount, pointsOverride, configuration);

        if (await TryConsumeZenAndItemsAsync(
                context,
                character,
                progression.RequiredZen,
                configuration.RequiredResetItem,
                progression.RequiredItemAmount,
                cancellationToken).ConfigureAwait(false) is { } costFailure)
        {
            return (false, costFailure);
        }

        var previousPoints = character.LevelUpPoints;
        WebCharacterMapper.SetAttributeValue(context, gameConfig, character, Stats.Resets, progression.NextResetCount);
        WebCharacterMapper.SetAttributeValue(context, gameConfig, character, Stats.Level, configuration.LevelAfterReset);
        character.Experience = GameContext.CalculateExperienceForLevel(gameConfig, configuration.LevelAfterReset);

        if (configuration.ResetStats && character.CharacterClass is not null)
        {
            foreach (var statDef in character.CharacterClass.StatAttributes.Where(s => s.IncreasableByPlayer && s.Attribute is not null))
            {
                WebCharacterMapper.SetAttributeValue(context, gameConfig, character, statDef.Attribute!, statDef.BaseValue);
            }
        }

        character.LevelUpPoints = configuration.ReplacePointsPerReset
            ? progression.TotalPointsAfterReset
            : previousPoints + progression.PointsForReset;

        if (configuration.MoveHome)
        {
            MoveCharacterToHomeMap(character);
        }

        var pointsDelta = character.LevelUpPoints - previousPoints;
        return (true, new WebCharacterOperationResult(
            WebCharacterOperationStatus.Success,
            $"Reset #{progression.NextResetCount} completed.",
            WebCharacterMapper.ToWebCharacterInfo(character),
            progression.RequiredZen,
            pointsDelta));
    }

    private (bool ShouldSave, WebCharacterOperationResult Result) ApplyPkClear(
        GameConfiguration gameConfig,
        Character character)
    {
        var (isActive, configuration) = WebCharacterMapper.ResolvePlugInConfiguration<PkClearChatCommandPlugIn, PkClearChatCommandPlugIn.PKClearConfiguration>(
            gameConfig,
            this._gameConfigurationSource);
        if (!isActive || !configuration.AllowRegularPlayers)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.FeatureDisabled, "Self-service PK clear is disabled."));
        }

        if (character.PlayerKillCount == 0 && character.State < HeroState.PlayerKillWarning)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.NotPlayerKiller, "Character does not have PK status."));
        }

        var costLong = (long)Math.Max(0, character.PlayerKillCount) * Math.Max(0, configuration.ZenCostPerKill);
        var requiredZen = (int)Math.Min(int.MaxValue, costLong);
        var currentMoney = character.Inventory?.Money ?? 0;
        if (requiredZen > 0 && (character.Inventory is null || currentMoney < requiredZen))
        {
            return (false, new WebCharacterOperationResult(
                WebCharacterOperationStatus.InsufficientZen,
                $"Insufficient Zen to clear PK (requires {requiredZen:N0}, current {currentMoney:N0})."));
        }

        if (requiredZen > 0 && character.Inventory is not null)
        {
            character.Inventory.Money -= requiredZen;
        }

        character.State = HeroState.Normal;
        character.StateRemainingSeconds = 0;
        character.PlayerKillCount = 0;

        return (true, new WebCharacterOperationResult(
            WebCharacterOperationStatus.Success,
            "PK status cleared.",
            WebCharacterMapper.ToWebCharacterInfo(character),
            requiredZen));
    }

    private async ValueTask<(bool ShouldSave, WebCharacterOperationResult Result)> ApplyStatResetAsync(
        IContext context,
        GameConfiguration gameConfig,
        Character character,
        CancellationToken cancellationToken)
    {
        var (isActive, configuration) = WebCharacterMapper.ResolvePlugInConfiguration<StatResetFeaturePlugIn, StatResetConfiguration>(
            gameConfig,
            this._gameConfigurationSource);
        if (!isActive)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.FeatureDisabled, "Stat reset feature is not enabled on this server."));
        }

        var level = (int)WebCharacterMapper.GetAttributeValue(character, Stats.Level, 1f);
        if (level < configuration.RequiredLevel)
        {
            return (false, new WebCharacterOperationResult(
                WebCharacterOperationStatus.InsufficientLevel,
                $"Character level {level} is below required level {configuration.RequiredLevel}."));
        }

        if (character.CharacterClass is null)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.InvalidInput, "Character class definition is missing."));
        }

        var increasableStats = character.CharacterClass.StatAttributes
            .Where(s => s.IncreasableByPlayer && s.Attribute is not null)
            .ToArray();
        var pendingRefund = 0;
        foreach (var statDef in increasableStats)
        {
            var currentValue = (int)WebCharacterMapper.GetAttributeValue(character, statDef.Attribute!, statDef.BaseValue);
            var baseValue = (int)statDef.BaseValue;
            pendingRefund += Math.Max(0, currentValue - baseValue);
        }

        if (pendingRefund == 0)
        {
            return (false, new WebCharacterOperationResult(WebCharacterOperationStatus.NoStatsToReset, "No invested stat points to refund."));
        }

        var requiredZen = Math.Max(0, configuration.RequiredMoney);
        if (await TryConsumeZenAndItemsAsync(
                context,
                character,
                requiredZen,
                configuration.RequiredResetItem,
                configuration.RequiredResetItem is not null ? 1 : 0,
                cancellationToken).ConfigureAwait(false) is { } costFailure)
        {
            return (false, costFailure);
        }

        foreach (var statDef in increasableStats)
        {
            WebCharacterMapper.SetAttributeValue(context, gameConfig, character, statDef.Attribute!, statDef.BaseValue);
        }

        character.LevelUpPoints += pendingRefund;
        if (configuration.MoveHome)
        {
            MoveCharacterToHomeMap(character);
        }

        return (true, new WebCharacterOperationResult(
            WebCharacterOperationStatus.Success,
            $"Refunded {pendingRefund} stat points.",
            WebCharacterMapper.ToWebCharacterInfo(character),
            requiredZen,
            pendingRefund));
    }

    private async ValueTask<WebCharacterOperationResult> ExecuteOfflineCharacterMutationAsync(
        string loginName,
        string characterName,
        Func<IContext, GameConfiguration, Character, ValueTask<(bool ShouldSave, WebCharacterOperationResult Result)>> mutator,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = (loginName ?? string.Empty).Trim();
        var normalizedChar = (characterName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedLogin) || string.IsNullOrEmpty(normalizedChar))
        {
            return new WebCharacterOperationResult(WebCharacterOperationStatus.InvalidInput, "LoginName and CharacterName are required.");
        }

        var config = await this._gameConfigurationSource.GetOwnerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        using (var probeContext = this._persistenceContextProvider.CreateNewPlayerContext(config))
        {
            var probeAccount = await probeContext.GetAccountByLoginNameAsync(normalizedLogin, cancellationToken).ConfigureAwait(false);
            if (probeAccount is null)
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.AccountNotFound, "Account not found.");
            }

            if (probeAccount.State is AccountState.Banned or AccountState.TemporarilyBanned)
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.AccountBanned, "Account is banned.");
            }

            if (probeAccount.Characters.All(c => !c.Name.Equals(normalizedChar, StringComparison.Ordinal)))
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.CharacterNotFound, "Character not found on this account.");
            }
        }

        var reservationId = WebCharacterMapper.CreateReservationId();
        if (!await this._loginServer.TryReserveAccountAsync(normalizedLogin, reservationId).ConfigureAwait(false))
        {
            return new WebCharacterOperationResult(
                WebCharacterOperationStatus.AccountOnline,
                "Please disconnect your account from the game server before modifying character data.");
        }

        try
        {
            using var editContext = this._persistenceContextProvider.CreateNewPlayerContext(config);
            var account = await editContext.GetAccountByLoginNameAsync(normalizedLogin, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.AccountNotFound, "Account not found.");
            }

            if (account.State is AccountState.Banned or AccountState.TemporarilyBanned)
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.AccountBanned, "Account is banned.");
            }

            var character = account.Characters.FirstOrDefault(c => c.Name.Equals(normalizedChar, StringComparison.Ordinal));
            if (character is null)
            {
                return new WebCharacterOperationResult(WebCharacterOperationStatus.CharacterNotFound, "Character not found on this account.");
            }

            var (shouldSave, result) = await mutator(editContext, config, character).ConfigureAwait(false);
            if (shouldSave)
            {
                await editContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation(
                    "Web offline character operation succeeded for '{LoginName}' / '{CharacterName}': {Message}",
                    account.LoginName,
                    character.Name,
                    result.Message);
            }

            return result;
        }
        finally
        {
            await this._loginServer.ReleaseAccountReservationAsync(normalizedLogin, reservationId).ConfigureAwait(false);
        }
    }
}
