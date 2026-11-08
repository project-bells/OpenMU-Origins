// <copyright file="WebShopAccountOperationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.WebShop;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests the exactly-once application of server-authoritative webshop operations.
/// </summary>
[TestFixture]
public class WebShopAccountOperationTests
{
    /// <summary>
    /// Online time is accrued monotonically in complete seconds without counting a partial second twice.
    /// </summary>
    [Test]
    public async ValueTask AccruesAuthoritativeOnlineTimeAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var account = player.PersistenceContext.CreateNew<Account>();
        account.LoginName = "online-time-test";
        await player.SetAccountAsync(account).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        player.AccrueOnlineTime(now.AddSeconds(65.8));
        player.AccrueOnlineTime(now.AddSeconds(66.2));

        Assert.That(account.OnlineTimeSeconds, Is.InRange(65, 66));
    }

    /// <summary>
    /// A repeated application does not add the purchased vault money twice.
    /// </summary>
    [Test]
    public async ValueTask AddsVaultMoneyExactlyOnceAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var account = player.PersistenceContext.CreateNew<Account>();
        account.LoginName = "webshop-test";
        account.Vault = player.PersistenceContext.CreateNew<ItemStorage>();
        player.Account = account;
        var operation = player.PersistenceContext.CreateNew<WebShopAccountOperation>();
        operation.AccountId = account.GetId();
        operation.OperationType = WebShopAccountOperationType.AddVaultMoney;
        operation.Amount = 500_000_000;
        operation.Reference = "webshop:zen:test-order";
        operation.CreatedAt = DateTime.UtcNow;
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);

        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);
        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);

        Assert.That(account.Vault.Money, Is.EqualTo(500_000_000));
        Assert.That(operation.AppliedAt, Is.Not.Null);
        Assert.That(operation.FailureCode, Is.Null);
    }

    /// <summary>
    /// A purchased level 400 character is initialized by the game server and created exactly once.
    /// </summary>
    [Test]
    public async ValueTask CreatesLevel400CharacterExactlyOnceAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var account = player.PersistenceContext.CreateNew<Account>();
        account.LoginName = "webshop-character-test";
        player.Account = account;
        var characterClass = CreateCharacterClass(player, true);
        var operation = player.PersistenceContext.CreateNew<WebShopAccountOperation>();
        operation.AccountId = account.GetId();
        operation.OperationType = WebShopAccountOperationType.CreateLevel400Character;
        operation.CharacterName = "ShopHero";
        operation.CharacterClassNumber = characterClass.Number;
        operation.Reference = "webshop:character:test-order";
        operation.CreatedAt = DateTime.UtcNow;
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);

        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);
        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);

        Assert.That(account.Characters, Has.Count.EqualTo(1));
        Assert.That(account.Characters.Single().Name, Is.EqualTo("ShopHero"));
        Assert.That(account.Characters.Single().Attributes.First(attribute => attribute.Definition == Stats.Level).Value, Is.EqualTo(400));
        Assert.That(operation.AppliedAt, Is.Not.Null);
        Assert.That(operation.FailureCode, Is.Null);
    }

    /// <summary>
    /// A rename is applied exactly once, while a definitive failure is persisted and not retried.
    /// </summary>
    [Test]
    public async ValueTask RenamesCharacterAndPersistsFailureExactlyOnceAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var account = player.PersistenceContext.CreateNew<Account>();
        account.LoginName = "webshop-rename-test";
        player.Account = account;
        var characterClass = CreateCharacterClass(player, false);
        var character = player.PersistenceContext.CreateNew<Character>();
        character.Name = "OldHero";
        character.CharacterSlot = 0;
        player.PersistenceContext.InitializeNewCharacter(character, characterClass);
        account.Characters.Add(character);
        var rename = player.PersistenceContext.CreateNew<WebShopAccountOperation>();
        rename.AccountId = account.GetId();
        rename.OperationType = WebShopAccountOperationType.RenameCharacter;
        rename.CharacterName = "OldHero";
        rename.NewCharacterName = "NewHero";
        rename.Reference = "webshop:rename:test-order";
        rename.CreatedAt = DateTime.UtcNow;
        var missing = player.PersistenceContext.CreateNew<WebShopAccountOperation>();
        missing.AccountId = account.GetId();
        missing.OperationType = WebShopAccountOperationType.RenameCharacter;
        missing.CharacterName = "Missing";
        missing.NewCharacterName = "NeverUsed";
        missing.Reference = "webshop:rename:failed-order";
        missing.CreatedAt = DateTime.UtcNow;
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);

        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);
        await player.ApplyPendingWebShopAccountOperationsAsync().ConfigureAwait(false);

        Assert.That(character.Name, Is.EqualTo("NewHero"));
        Assert.That(rename.AppliedAt, Is.Not.Null);
        Assert.That(rename.FailureCode, Is.Null);
        Assert.That(missing.AppliedAt, Is.Null);
        Assert.That(missing.FailureCode, Is.EqualTo("CHARACTER_NOT_FOUND"));
    }

    private static CharacterClass CreateCharacterClass(Player player, bool isMasterClass)
    {
        player.GameContext.Configuration.MaximumCharactersPerAccount = 5;
        var characterClass = player.PersistenceContext.CreateNew<CharacterClass>();
        characterClass.Number = isMasterClass ? (byte)19 : (byte)0;
        characterClass.IsMasterClass = isMasterClass;
        characterClass.HomeMap = player.PersistenceContext.CreateNew<GameMapDefinition>();
        characterClass.StatAttributes.Add(player.PersistenceContext.CreateNew<StatAttributeDefinition>(Stats.Level, 1, false));
        characterClass.StatAttributes.Add(player.PersistenceContext.CreateNew<StatAttributeDefinition>(Stats.PointsPerLevelUp, 5, false));
        player.GameContext.Configuration.CharacterClasses.Add(characterClass);
        return characterClass;
    }
}
