// <copyright file="EnterMiniGameTicketConsumptionTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;

/// <summary>
/// Tests ticket validation and consumption timing for shared-map mini games.
/// </summary>
[TestFixture]
public class EnterMiniGameTicketConsumptionTests
{
    private const byte TicketSlot = 12;
    private const byte TicketLevel = 1;
    private const byte InitialDurability = 2;

    private readonly List<MiniGameContext> _gamesToDispose = new();

    /// <summary>
    /// Disposes mini games created by the tests.
    /// </summary>
    [TearDown]
    public async Task TearDownAsync()
    {
        foreach (var game in this._gamesToDispose)
        {
            await game.DisposeAsync().ConfigureAwait(false);
        }

        this._gamesToDispose.Clear();
    }

    /// <summary>
    /// Tests that a valid invitation is consumed only after the player enters successfully.
    /// </summary>
    [TestCase(MiniGameType.BloodCastle, 13, 18)]
    [TestCase(MiniGameType.DevilSquare, 14, 19)]
    public async Task SuccessfulEntry_ConsumesOneTicketUseAsync(MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition(player, gameType, ticketGroup, ticketNumber);
        var ticket = await AddTicketAsync(player, definition.TicketItem!, TicketLevel).ConfigureAwait(false);

        await new EnterMiniGameAction().TryEnterMiniGameAsync(player, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);

        Assert.That(player.CurrentMiniGame, Is.Not.Null);
        Assert.That(ticket.Durability, Is.EqualTo(InitialDurability - 1));
        this._gamesToDispose.Add(player.CurrentMiniGame!);
    }

    /// <summary>
    /// Tests that a valid invitation isn't consumed when the character level is rejected.
    /// </summary>
    [TestCase(MiniGameType.BloodCastle, 13, 18)]
    [TestCase(MiniGameType.DevilSquare, 14, 19)]
    public async Task LevelFailure_DoesNotConsumeTicketAsync(MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition(player, gameType, ticketGroup, ticketNumber);
        definition.MinimumCharacterLevel = 101;
        player.Attributes![Stats.Level] = 100;
        var ticket = await AddTicketAsync(player, definition.TicketItem!, TicketLevel).ConfigureAwait(false);

        await new EnterMiniGameAction().TryEnterMiniGameAsync(player, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);

        Assert.That(player.CurrentMiniGame, Is.Null);
        Assert.That(ticket.Durability, Is.EqualTo(InitialDurability));
        VerifyEnterResult(player, gameType, EnterResult.CharacterLevelTooLow);
    }

    /// <summary>
    /// Tests that an invitation for another event level is rejected without consumption.
    /// </summary>
    [TestCase(MiniGameType.BloodCastle, 13, 18)]
    [TestCase(MiniGameType.DevilSquare, 14, 19)]
    public async Task WrongTicketLevel_DoesNotConsumeTicketAsync(MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition(player, gameType, ticketGroup, ticketNumber);
        var ticket = await AddTicketAsync(player, definition.TicketItem!, TicketLevel + 1).ConfigureAwait(false);

        await new EnterMiniGameAction().TryEnterMiniGameAsync(player, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);

        Assert.That(player.CurrentMiniGame, Is.Null);
        Assert.That(ticket.Durability, Is.EqualTo(InitialDurability));
        VerifyEnterResult(player, gameType, EnterResult.Failed);
    }

    /// <summary>
    /// Tests that a valid invitation isn't consumed when the event rejects the player because it is full.
    /// </summary>
    [TestCase(MiniGameType.BloodCastle, 13, 18)]
    [TestCase(MiniGameType.DevilSquare, 14, 19)]
    public async Task FullEvent_DoesNotConsumeRejectedPlayersTicketAsync(MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        var firstPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition(firstPlayer, gameType, ticketGroup, ticketNumber);
        var firstTicket = await AddTicketAsync(firstPlayer, definition.TicketItem!, TicketLevel).ConfigureAwait(false);
        var action = new EnterMiniGameAction();
        await action.TryEnterMiniGameAsync(firstPlayer, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);
        Assert.That(firstPlayer.CurrentMiniGame, Is.Not.Null);
        this._gamesToDispose.Add(firstPlayer.CurrentMiniGame!);

        var rejectedPlayer = await PlayerTestHelper.CreatePlayerAsync(firstPlayer.GameContext).ConfigureAwait(false);
        rejectedPlayer.Attributes![Stats.Level] = 100;
        var rejectedTicket = await AddTicketAsync(rejectedPlayer, definition.TicketItem!, TicketLevel).ConfigureAwait(false);

        await action.TryEnterMiniGameAsync(rejectedPlayer, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);

        Assert.That(firstTicket.Durability, Is.EqualTo(InitialDurability - 1));
        Assert.That(rejectedPlayer.CurrentMiniGame, Is.Null);
        Assert.That(rejectedTicket.Durability, Is.EqualTo(InitialDurability));
        VerifyEnterResult(rejectedPlayer, gameType, EnterResult.Full);
    }

    /// <summary>
    /// Tests that entering Blood Castle or Devil Square keeps the player's party membership.
    /// </summary>
    [TestCase(MiniGameType.BloodCastle, 13, 18)]
    [TestCase(MiniGameType.DevilSquare, 14, 19)]
    public async Task SuccessfulEntry_KeepsPartyMembershipAsync(MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var partyMember = await PlayerTestHelper.CreatePlayerAsync(player.GameContext).ConfigureAwait(false);
        var party = new Party(new PartyManager(5, new NullLogger<Party>()), 5, new NullLogger<Party>());
        await party.AddAsync(player).ConfigureAwait(false);
        await party.AddAsync(partyMember).ConfigureAwait(false);
        var definition = CreateDefinition(player, gameType, ticketGroup, ticketNumber);
        await AddTicketAsync(player, definition.TicketItem!, TicketLevel).ConfigureAwait(false);

        await new EnterMiniGameAction().TryEnterMiniGameAsync(player, gameType, definition.GameLevel, TicketSlot).ConfigureAwait(false);

        Assert.That(player.CurrentMiniGame, Is.Not.Null);
        Assert.That(player.Party, Is.SameAs(party));
        Assert.That(party.PartyList, Does.Contain(player));
        Assert.That(party.PartyList, Does.Contain(partyMember));
        this._gamesToDispose.Add(player.CurrentMiniGame!);
    }

    /// <summary>
    /// Tests that entering Chaos Castle removes only the entrant from its party and prevents regrouping inside the event.
    /// </summary>
    [Test]
    public async Task ChaosCastleEntry_RemovesEntrantFromPartyAndPreventsRegroupingAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var firstPartyMember = await PlayerTestHelper.CreatePlayerAsync(player.GameContext).ConfigureAwait(false);
        var secondPartyMember = await PlayerTestHelper.CreatePlayerAsync(player.GameContext).ConfigureAwait(false);
        var party = new Party(new PartyManager(5, new NullLogger<Party>()), 5, new NullLogger<Party>());
        await party.AddAsync(player).ConfigureAwait(false);
        await party.AddAsync(firstPartyMember).ConfigureAwait(false);
        await party.AddAsync(secondPartyMember).ConfigureAwait(false);
        var definition = CreateDefinition(player, MiniGameType.ChaosCastle, 13, 29);
        definition.AllowParty = false;
        await AddTicketAsync(player, definition.TicketItem!, TicketLevel).ConfigureAwait(false);

        await new EnterMiniGameAction().TryEnterMiniGameAsync(player, MiniGameType.ChaosCastle, definition.GameLevel, TicketSlot).ConfigureAwait(false);
        await new PartyRequestAction().HandlePartyRequestAsync(player, firstPartyMember).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(player.CurrentMiniGame, Is.Not.Null);
            Assert.That(player.Party, Is.Null);
            Assert.That(party.PartyList, Is.EqualTo(new[] { firstPartyMember, secondPartyMember }));
            Assert.That(firstPartyMember.Party, Is.SameAs(party));
            Assert.That(secondPartyMember.Party, Is.SameAs(party));
            Assert.That(firstPartyMember.LastPartyRequester, Is.Null);
        });
        this._gamesToDispose.Add(player.CurrentMiniGame!);
    }

    private static MiniGameDefinition CreateDefinition(Player player, MiniGameType gameType, byte ticketGroup, short ticketNumber)
    {
        player.Attributes![Stats.Level] = 100;
        var map = player.GameContext.Configuration.Maps.First();
        var ticketDefinition = new ItemDefinition
        {
            Group = ticketGroup,
            Number = ticketNumber,
            Width = 1,
            Height = 1,
            Durability = InitialDurability,
        };
        player.GameContext.Configuration.Items.Add(ticketDefinition);

        var definitionMock = new Mock<MiniGameDefinition>();
        definitionMock.SetupAllProperties();
        definitionMock.SetupGet(definition => definition.Rewards).Returns(new List<MiniGameReward>());
        definitionMock.SetupGet(definition => definition.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definitionMock.SetupGet(definition => definition.ChangeEvents).Returns(new List<MiniGameChangeEvent>());
        var definition = definitionMock.Object;
        definition.Type = gameType;
        definition.GameLevel = TicketLevel;
        definition.TicketItem = ticketDefinition;
        definition.TicketItemLevel = TicketLevel;
        definition.MinimumCharacterLevel = 1;
        definition.MaximumCharacterLevel = 400;
        definition.MinimumSpecialCharacterLevel = 1;
        definition.MaximumSpecialCharacterLevel = 400;
        definition.MaximumPlayerCount = 1;
        definition.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
        definition.AllowParty = true;
        definition.Entrance = new ExitGate { Map = map };
        definition.EnterDuration = TimeSpan.FromMinutes(1);
        definition.GameDuration = TimeSpan.FromMinutes(1);
        definition.ExitDuration = TimeSpan.FromMinutes(1);

        var definitions = new List<MiniGameDefinition> { definition };
        Mock.Get(player.GameContext.Configuration)
            .SetupGet(configuration => configuration.MiniGameDefinitions)
            .Returns(definitions);
        return definition;
    }

    private static async Task<Item> AddTicketAsync(Player player, ItemDefinition definition, byte level)
    {
        var ticket = new Item
        {
            Definition = definition,
            Durability = InitialDurability,
            ItemSlot = TicketSlot,
            Level = level,
        };
        await player.Inventory!.AddItemAsync(TicketSlot, ticket).ConfigureAwait(false);
        return ticket;
    }

    private static void VerifyEnterResult(Player player, MiniGameType gameType, EnterResult result)
    {
        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowMiniGameEnterResultPlugIn>()!)
            .Verify(plugin => plugin.ShowResultAsync(gameType, result), Times.Once);
    }
}
