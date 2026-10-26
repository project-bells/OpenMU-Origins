// <copyright file="SkillCastAdmissionGuardTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.AntiCheat;
using NUnit.Framework;

/// <summary>
/// Tests the server-side per-skill cast admission budget.
/// </summary>
[TestFixture]
public class SkillCastAdmissionGuardTests
{
    /// <summary>
    /// Verifies that the configured short burst is accepted.
    /// </summary>
    [Test]
    public void TryAdmit_WithinBurst_AllowsCasts()
    {
        var guard = new SkillCastAdmissionGuard();
        var now = DateTimeOffset.UtcNow;
        Assert.Multiple(() =>
        {
            Assert.That(guard.TryAdmit(10, now, 25, 3), Is.True);
            Assert.That(guard.TryAdmit(10, now, 25, 3), Is.True);
            Assert.That(guard.TryAdmit(10, now, 25, 3), Is.True);
        });
    }

    /// <summary>
    /// Verifies that a sustained same-skill flood is rejected.
    /// </summary>
    [Test]
    public void TryAdmit_OverBurst_RejectsCast()
    {
        var guard = new SkillCastAdmissionGuard();
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            Assert.That(guard.TryAdmit(10, now, 25, 3), Is.True);
        }

        Assert.That(guard.TryAdmit(10, now, 25, 3), Is.False);
    }

    /// <summary>
    /// Verifies that different skills have independent budgets.
    /// </summary>
    [Test]
    public void TryAdmit_DifferentSkill_HasIndependentBudget()
    {
        var guard = new SkillCastAdmissionGuard();
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            Assert.That(guard.TryAdmit(10, now, 25, 3), Is.True);
        }

        Assert.That(guard.TryAdmit(11, now, 25, 3), Is.True);
    }

    /// <summary>
    /// Verifies that elapsed server time refills the cast budget.
    /// </summary>
    [Test]
    public void TryAdmit_AfterInterval_AllowsCastAgain()
    {
        var guard = new SkillCastAdmissionGuard();
        var now = DateTimeOffset.UtcNow;
        Assert.That(guard.TryAdmit(10, now, 25, 1), Is.True);
        Assert.That(guard.TryAdmit(10, now, 25, 1), Is.False);
        Assert.That(guard.TryAdmit(10, now.AddMilliseconds(25), 25, 1), Is.True);
    }
}
