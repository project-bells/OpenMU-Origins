// <copyright file="PacketAdmissionGuardTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.GameServer.RemoteView;
using NUnit.Framework;

/// <summary>
/// Tests for the packet admission token bucket.
/// </summary>
[TestFixture]
public class PacketAdmissionGuardTests
{
    [Test]
    public async Task TryAdmit_WithinBurst_AllowsPacket()
    {
        var guard = await CreateGuardAsync(new ServerAuthoritativeAntiCheatConfiguration { ObserveOnly = false, PacketBurstCapacity = 2, PacketsPerSecond = 1 });
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        Assert.That(guard.TryAdmit(now, 4, out _), Is.True);
        Assert.That(guard.TryAdmit(now, 4, out _), Is.True);
    }

    [Test]
    public async Task TryAdmit_OverBudgetInEnforcementMode_RejectsPacket()
    {
        var guard = await CreateGuardAsync(new ServerAuthoritativeAntiCheatConfiguration { ObserveOnly = false, PacketBurstCapacity = 1, PacketsPerSecond = 1 });
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        guard.TryAdmit(now, 4, out _);

        Assert.That(guard.TryAdmit(now, 4, out var decision), Is.False);
        Assert.That(decision?.RejectAction, Is.True);
    }

    [Test]
    public async Task TryAdmit_OverBudgetInObserveMode_AllowsPacketAndScoresEvidence()
    {
        var guard = await CreateGuardAsync(new ServerAuthoritativeAntiCheatConfiguration { ObserveOnly = true, PacketBurstCapacity = 1, PacketsPerSecond = 1 });
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        guard.TryAdmit(now, 4, out _);

        Assert.That(guard.TryAdmit(now, 4, out var decision), Is.True);
        Assert.That(decision?.Level, Is.EqualTo(AntiCheatRiskLevel.Low));
    }

    [Test]
    public async Task TryAdmit_AfterRefill_AllowsPacketAgain()
    {
        var guard = await CreateGuardAsync(new ServerAuthoritativeAntiCheatConfiguration { ObserveOnly = false, PacketBurstCapacity = 1, PacketsPerSecond = 1 });
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        guard.TryAdmit(now, 4, out _);

        Assert.That(guard.TryAdmit(now.AddSeconds(1), 4, out _), Is.True);
    }

    private static async ValueTask<PacketAdmissionGuard> CreateGuardAsync(ServerAuthoritativeAntiCheatConfiguration configuration)
    {
        var antiCheat = new ServerAuthoritativeAntiCheatPlugIn { Configuration = configuration };
        return new PacketAdmissionGuard(await PlayerTestHelper.CreatePlayerAsync(), antiCheat);
    }
}
