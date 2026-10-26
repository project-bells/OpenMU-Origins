// <copyright file="EvolveCharacterChatCommandPlugInTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using NUnit.Framework;

/// <summary>
/// Tests the eligibility rules of the Class 3 evolution command.
/// </summary>
[TestFixture]
public class EvolveCharacterChatCommandPlugInTests
{
    /// <summary>
    /// Verifies that a level 400 second-generation class can evolve to its master class.
    /// </summary>
    [Test]
    public void CanEvolve_Level400WithMasterSuccessor_ReturnsTrue()
    {
        var current = new CharacterClass { NextGenerationClass = new CharacterClass { IsMasterClass = true } };
        Assert.That(EvolveCharacterChatCommandPlugIn.CanEvolve(current, 400), Is.True);
    }

    /// <summary>
    /// Verifies that a character below level 400 cannot evolve.
    /// </summary>
    [Test]
    public void CanEvolve_Level399_ReturnsFalse()
    {
        var current = new CharacterClass { NextGenerationClass = new CharacterClass { IsMasterClass = true } };
        Assert.That(EvolveCharacterChatCommandPlugIn.CanEvolve(current, 399), Is.False);
    }

    /// <summary>
    /// Verifies that a level above the exact requirement is not accepted.
    /// </summary>
    [Test]
    public void CanEvolve_Level401_ReturnsFalse()
    {
        var current = new CharacterClass { NextGenerationClass = new CharacterClass { IsMasterClass = true } };
        Assert.That(EvolveCharacterChatCommandPlugIn.CanEvolve(current, 401), Is.False);
    }

    /// <summary>
    /// Verifies that the command cannot be used for first-to-second evolution.
    /// </summary>
    [Test]
    public void CanEvolve_NonMasterSuccessor_ReturnsFalse()
    {
        var current = new CharacterClass { NextGenerationClass = new CharacterClass { IsMasterClass = false } };
        Assert.That(EvolveCharacterChatCommandPlugIn.CanEvolve(current, 400), Is.False);
    }

    /// <summary>
    /// Verifies that an already-final class cannot evolve again.
    /// </summary>
    [Test]
    public void CanEvolve_NoSuccessor_ReturnsFalse()
        => Assert.That(EvolveCharacterChatCommandPlugIn.CanEvolve(new CharacterClass(), 400), Is.False);
}
