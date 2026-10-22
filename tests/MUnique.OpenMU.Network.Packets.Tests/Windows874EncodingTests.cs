// <copyright file="Windows874EncodingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.Packets.Tests;

/// <summary>
/// Tests the Thai wire encoding used by classic ten-byte identity fields.
/// </summary>
[TestFixture]
public class Windows874EncodingTests
{
    /// <summary>
    /// Ensures that supported Thai character names use one byte per code point and round-trip.
    /// </summary>
    /// <param name="name">The character name.</param>
    [TestCase("สุดเขต")]
    [TestCase("สมยศ")]
    [TestCase("เพลินจิต")]
    public void ThaiNameRoundTrips(string name)
    {
        byte[] bytes = Windows874Encoding.Instance.GetBytes(name);

        Assert.That(bytes, Has.Length.EqualTo(name.Length));
        Assert.That(Windows874Encoding.Instance.GetString(bytes), Is.EqualTo(name));
    }
}
