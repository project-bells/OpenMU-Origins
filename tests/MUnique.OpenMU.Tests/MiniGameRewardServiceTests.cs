// <copyright file="MiniGameRewardServiceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// Tests the <see cref="MiniGameRewardService"/>.
/// </summary>
[TestFixture]
public class MiniGameRewardServiceTests
{
    /// <summary>
    /// Ensures that an unsupported persisted reward value doesn't abort the remaining rewards.
    /// </summary>
    [Test]
    public async Task UnsupportedRewardTypeIsSkippedAsync()
    {
        const int score = 50;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = new Mock<MiniGameDefinition>();
        definition.Setup(game => game.Rewards).Returns(new List<MiniGameReward>
        {
            new() { RewardType = (MiniGameRewardType)byte.MaxValue },
            new() { RewardType = MiniGameRewardType.Score, RewardAmount = score },
        });
        var service = new MiniGameRewardService(
            definition.Object,
            player.GameContext!,
            player.CurrentMap!,
            () => NullDropGenerator.Instance,
            () => TimeSpan.Zero,
            () => null,
            NullLogger.Instance,
            this);

        var (bonusScore, givenMoney) = await service.GiveRewardsAndGetBonusScoreAsync(player, 1).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(bonusScore, Is.EqualTo(score));
            Assert.That(givenMoney, Is.Zero);
        });
    }

    /// <summary>
    /// Ensures that a rejected money reward isn't reported as given in the event result.
    /// </summary>
    [Test]
    public async Task MoneyRewardAtInventoryMoneyLimitIsNotReportedAsGivenAsync()
    {
        const int maximumMoney = 100;
        const int rewardAmount = 50;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext!.Configuration.MaximumInventoryMoney = maximumMoney;
        player.Money = maximumMoney;

        var definition = new Mock<MiniGameDefinition>();
        definition.Setup(game => game.Rewards).Returns(new List<MiniGameReward>
        {
            new()
            {
                RewardType = MiniGameRewardType.Money,
                RewardAmount = rewardAmount,
            },
        });
        var service = new MiniGameRewardService(
            definition.Object,
            player.GameContext,
            player.CurrentMap!,
            () => NullDropGenerator.Instance,
            () => TimeSpan.Zero,
            () => null,
            NullLogger.Instance,
            this);

        var (_, givenMoney) = await service.GiveRewardsAndGetBonusScoreAsync(player, 1).ConfigureAwait(false);

        Assert.That(player.Money, Is.EqualTo(maximumMoney));
        Assert.That(givenMoney, Is.Zero);
    }
}
