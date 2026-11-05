// <copyright file="MuHelperSettingsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text;
using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.MuHelper;
using MUnique.OpenMU.GameServer.MessageHandler.MuHelper;
using MUnique.OpenMU.GameServer.RemoteView.MuHelper;
using MUnique.OpenMU.Network.Packets.ClientToServer;

/// <summary>
/// Tests persistence and decoding of the ENG 1.04d MU Helper settings payload.
/// </summary>
[TestFixture]
public class MuHelperSettingsTests
{
    /// <summary>
    /// Tests that the exact client payload is saved and echoed without mutation.
    /// </summary>
    [Test]
    public async Task SaveEchoesExactPayloadAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var payload = CreatePayload();
        var packet = new byte[MuHelperSaveDataRequest.Length];
        payload.CopyTo(packet, 4);
        Memory<byte> echoed = default;
        Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperConfigurationUpdatePlugIn>()!)
            .Setup(view => view.UpdateMuHelperConfigurationAsync(It.IsAny<Memory<byte>>()))
            .Callback<Memory<byte>>(data => echoed = data)
            .Returns(ValueTask.CompletedTask);

        await new MuHelperSaveDataRequestHandlerPlugin().HandlePacketAsync(player, packet).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(player.SelectedCharacter!.MuHelperConfiguration, Is.EqualTo(payload));
            Assert.That(echoed.ToArray(), Is.EqualTo(payload));
        });
    }

    /// <summary>
    /// Tests that truncated and oversized payloads cannot replace persisted settings.
    /// </summary>
    /// <param name="length">The invalid payload length.</param>
    [TestCase(0)]
    [TestCase(260)]
    [TestCase(262)]
    public async Task InvalidPacketLengthIsRejectedAsync(int length)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var original = CreatePayload();
        player.SelectedCharacter!.MuHelperConfiguration = original;

        await new MuHelperSaveDataRequestHandlerPlugin().HandlePacketAsync(player, new byte[length]).ConfigureAwait(false);

        Assert.That(player.SelectedCharacter.MuHelperConfiguration, Is.SameAs(original));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IMuHelperConfigurationUpdatePlugIn>()!)
            .Verify(view => view.UpdateMuHelperConfigurationAsync(It.IsAny<Memory<byte>>()), Times.Never());
    }

    /// <summary>
    /// Tests that truncated and oversized settings blobs cannot be decoded.
    /// </summary>
    /// <param name="length">The invalid blob length.</param>
    [TestCase(0)]
    [TestCase(69)]
    [TestCase(256)]
    [TestCase(258)]
    public void InvalidBlobLengthIsRejected(int length)
    {
        Assert.That(MuHelperSettingsSerializer.TryDeserialize(new byte[length]), Is.Null);
    }

    /// <summary>
    /// Tests all decoded skill, buff, recovery, movement, repair, and pickup fields.
    /// </summary>
    [Test]
    public void PayloadDecodesAllRuntimeSettings()
    {
        var settings = (MuHelperSettings)MuHelperSettingsSerializer.TryDeserialize(CreatePayload())!;

        Assert.Multiple(() =>
        {
            Assert.That((settings.BasicSkillId, settings.ActivationSkill1Id, settings.ActivationSkill2Id), Is.EqualTo((100, 200, 300)));
            Assert.That((settings.DelayMinSkill1, settings.DelayMinSkill2), Is.EqualTo((11, 22)));
            Assert.That((settings.Skill1UseTimer, settings.Skill1UseCondition, settings.Skill1ConditionAttacking, settings.Skill1SubCondition), Is.EqualTo((true, true, true, 2)));
            Assert.That((settings.Skill2UseTimer, settings.Skill2UseCondition, settings.Skill2ConditionAttacking, settings.Skill2SubCondition), Is.EqualTo((true, true, true, 3)));
            Assert.That((settings.BuffSkill0Id, settings.BuffSkill1Id, settings.BuffSkill2Id), Is.EqualTo((400, 500, 600)));
            Assert.That((settings.BuffOnDuration, settings.BuffDurationForParty, settings.BuffCastIntervalSeconds), Is.EqualTo((true, true, 33)));
            Assert.That((settings.AutoHeal, settings.HealThresholdPercent, settings.UseDrainLife), Is.EqualTo((true, 40, true)));
            Assert.That((settings.UseHealPotion, settings.PotionThresholdPercent), Is.EqualTo((true, 30)));
            Assert.That((settings.SupportParty, settings.AutoHealParty, settings.HealPartyThresholdPercent), Is.EqualTo((true, true, 50)));
            Assert.That((settings.HuntingRange, settings.ObtainRange, settings.MaxSecondsAway), Is.EqualTo((7, 9, 44)));
            Assert.That((settings.LongRangeCounterAttack, settings.ReturnToOriginalPosition, settings.UseCombo), Is.EqualTo((true, true, true)));
            Assert.That((settings.UseDarkRaven, settings.DarkRavenMode), Is.EqualTo((true, 2)));
            Assert.That((settings.PickAllItems, settings.PickSelectItems, settings.PickJewel, settings.PickZen), Is.EqualTo((true, true, true, true)));
            Assert.That((settings.PickAncient, settings.PickExcellent, settings.PickExtraItems), Is.EqualTo((true, true, true)));
            Assert.That(settings.ExtraItemNames, Is.EqualTo(new[] { "Jewel of Bless", "Dragon" }));
            Assert.That((settings.RepairItem, settings.UseSelfDefense, settings.AutoAcceptFriend, settings.AutoAcceptGuild, settings.FallbackBasicAttack), Is.EqualTo((true, true, true, true, true)));
        });
    }

    /// <summary>
    /// Tests that persisted settings are restored when the character reconnects and enters the world.
    /// </summary>
    [Test]
    public async Task EnteredWorldRestoresPersistedSettingsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.MuHelperConfiguration = CreatePayload();
        player.MuHelperSettings = null;

        await new MuHelperSettingsInitializationPlugIn()
            .PlayerStateChangedAsync(player, PlayerState.CharacterSelection, PlayerState.EnteredWorld).ConfigureAwait(false);

        Assert.That(player.MuHelperSettings, Is.TypeOf<MuHelperSettings>());
        Assert.That(player.MuHelperSettings!.BasicSkillId, Is.EqualTo(100));
    }

    private static byte[] CreatePayload()
    {
        var payload = new byte[MuHelperSettingsSerializer.BlobLength];
        payload[1] = 0b1111_1000;
        payload[2] = 0x97;
        WriteWord(payload, 3, 44);
        WriteWord(payload, 5, 100);
        WriteWord(payload, 7, 200);
        WriteWord(payload, 9, 11);
        WriteWord(payload, 11, 300);
        WriteWord(payload, 13, 22);
        WriteWord(payload, 15, 33);
        WriteWord(payload, 17, 400);
        WriteWord(payload, 19, 500);
        WriteWord(payload, 21, 600);
        payload[23] = 0x43;
        payload[24] = 0x05;
        payload[25] = 0xFF;
        payload[26] = 0b1011_1111;
        payload[27] = 0b1111_1111;
        payload[28] = 2;
        payload[29] = 0x0F;
        Encoding.ASCII.GetBytes("Jewel of Bless").CopyTo(payload, 65);
        Encoding.ASCII.GetBytes("Dragon").CopyTo(payload, 80);
        return payload;
    }

    private static void WriteWord(byte[] payload, int offset, ushort value)
    {
        payload[offset] = (byte)value;
        payload[offset + 1] = (byte)(value >> 8);
    }
}
