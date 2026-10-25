// <copyright file="AntiCheatRiskPolicyTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.AntiCheat;
using NUnit.Framework;

/// <summary>
/// Tests for the central anti-cheat risk policy.
/// </summary>
[TestFixture]
public class AntiCheatRiskPolicyTests
{
    [Test]
    public void Decay_AfterOneHalfLife_HalvesScore()
        => Assert.That(AntiCheatRiskPolicy.Decay(80, TimeSpan.FromMinutes(15)), Is.EqualTo(40).Within(0.001));

    [Test]
    public void Classify_ConfirmedScoreFromOneCategory_StaysHigh()
        => Assert.That(AntiCheatRiskPolicy.Classify(120, 1), Is.EqualTo(AntiCheatRiskLevel.High));

    [Test]
    public void Classify_ConfirmedScoreWithCorroboration_RequiresReview()
    {
        var decision = AntiCheatRiskPolicy.Decide(120, 2, true);
        Assert.Multiple(() =>
        {
            Assert.That(decision.Level, Is.EqualTo(AntiCheatRiskLevel.Confirmed));
            Assert.That(decision.RejectAction, Is.True);
            Assert.That(decision.Disconnect, Is.True);
            Assert.That(decision.RequiresManualReview, Is.True);
        });
    }

    [Test]
    public void Add_LegitimateDelay_DecaysEarlierEvidence()
    {
        var state = new AntiCheatRiskState();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        state.Add(new(AntiCheatCategory.PacketFrequency, 20, "packet.burst", "rate=120", start), false);

        var decision = state.Add(new(AntiCheatCategory.PacketFrequency, 1, "packet.burst", "rate=61", start.AddMinutes(15)), false);

        Assert.That(decision.Score, Is.EqualTo(11).Within(0.001));
        Assert.That(decision.Level, Is.EqualTo(AntiCheatRiskLevel.Low));
    }

    [Test]
    public void Add_OutOfOrderEvidence_Throws()
    {
        var state = new AntiCheatRiskState();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        state.Add(new(AntiCheatCategory.MovementSpeed, 10, "move.fast", "tiles=8", start), true);

        Assert.That(
            () => state.Add(new(AntiCheatCategory.AttackSpeed, 10, "attack.fast", "interval=5", start.AddSeconds(-1)), true),
            Throws.ArgumentException);
    }
}
