// <copyright file="EconomicTransactionAdmissionTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.AntiCheat;
using NUnit.Framework;

/// <summary>
/// Tests the server-side serialization of client-requested economic transactions.
/// </summary>
[TestFixture]
public class EconomicTransactionAdmissionTests
{
    /// <summary>
    /// Verifies that an overlapping transaction is rejected and contributes trade-integrity evidence.
    /// </summary>
    [Test]
    public async ValueTask OverlappingTransaction_IsRejectedAndScoredAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var antiCheat = new ServerAuthoritativeAntiCheatPlugIn
        {
            Configuration = new ServerAuthoritativeAntiCheatConfiguration
            {
                ConcurrentTransactionRiskPoints = 25,
            },
        };

        Assert.That(antiCheat.TryBeginEconomicTransaction(player, "first", DateTimeOffset.UtcNow, out var firstLease, out var firstDecision), Is.True);
        Assert.That(firstLease, Is.Not.Null);
        Assert.That(firstDecision, Is.Null);

        Assert.That(antiCheat.TryBeginEconomicTransaction(player, "second", DateTimeOffset.UtcNow, out var secondLease, out var secondDecision), Is.False);
        Assert.That(secondLease, Is.Null);
        Assert.That(secondDecision, Is.Not.Null);
        Assert.That(secondDecision!.Score, Is.EqualTo(25));

        firstLease!.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the lease allows the next transaction.
    /// </summary>
    [Test]
    public async ValueTask CompletedTransaction_ReleasesAdmissionSlotAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var antiCheat = new ServerAuthoritativeAntiCheatPlugIn
        {
            Configuration = new ServerAuthoritativeAntiCheatConfiguration(),
        };

        Assert.That(antiCheat.TryBeginEconomicTransaction(player, "first", DateTimeOffset.UtcNow, out var firstLease, out _), Is.True);
        firstLease!.Dispose();

        Assert.That(antiCheat.TryBeginEconomicTransaction(player, "second", DateTimeOffset.UtcNow, out var secondLease, out _), Is.True);
        secondLease!.Dispose();
    }

    /// <summary>
    /// Verifies that different players don't block each other's transactions.
    /// </summary>
    [Test]
    public async ValueTask DifferentPlayers_HaveIndependentAdmissionSlotsAsync()
    {
        var firstPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var secondPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var antiCheat = new ServerAuthoritativeAntiCheatPlugIn
        {
            Configuration = new ServerAuthoritativeAntiCheatConfiguration(),
        };

        Assert.That(antiCheat.TryBeginEconomicTransaction(firstPlayer, "first", DateTimeOffset.UtcNow, out var firstLease, out _), Is.True);
        Assert.That(antiCheat.TryBeginEconomicTransaction(secondPlayer, "second", DateTimeOffset.UtcNow, out var secondLease, out _), Is.True);

        firstLease!.Dispose();
        secondLease!.Dispose();
    }
}
