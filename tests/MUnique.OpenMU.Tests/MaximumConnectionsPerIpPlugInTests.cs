// <copyright file="MaximumConnectionsPerIpPlugInTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Threading.Tasks;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
using NUnit.Framework;

/// <summary>
/// Unit tests for the <see cref="MaximumConnectionsPerIpPlugIn"/>.
/// </summary>
[TestFixture]
public class MaximumConnectionsPerIpPlugInTests
{
    private GameContext _gameContext = null!;
    private MaximumConnectionsPerIpPlugIn _plugin = null!;

    /// <summary>
    /// Sets up the game context and the plug-in with a limit of three connections per IP address.
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        var dummyPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        this._gameContext = (GameContext)dummyPlayer.GameContext;
        this._plugin = new MaximumConnectionsPerIpPlugIn
        {
            Configuration = new MaximumConnectionsPerIpPlugInConfiguration
            {
                MaximumConnectionsPerIp = 3,
                ObserveOnly = false,
            },
        };
    }

    /// <summary>
    /// Tests that a joining player below the connection limit is not cancelled.
    /// </summary>
    [Test]
    public async ValueTask UnderLimitSucceedsAsync()
    {
        await this.AddAuthenticatedPlayersAsync(2, "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1").ConfigureAwait(false);

        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that a joining player at the connection limit is cancelled.
    /// </summary>
    [Test]
    public async ValueTask AtLimitCancelsTransitionAsync()
    {
        await this.AddAuthenticatedPlayersAsync(3, "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1").ConfigureAwait(false);

        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.True);
        Assert.That(joiningPlayer.LoginResultOverride, Is.EqualTo(GameLogic.Views.Login.LoginResult.ServerIsFull));
    }

    /// <summary>
    /// Tests that observe-only mode records the limit violation without rejecting the joining player.
    /// </summary>
    [Test]
    public async ValueTask AtLimitObserveOnlyDoesNotCancelAsync()
    {
        this._plugin.Configuration!.ObserveOnly = true;
        await this.AddAuthenticatedPlayersAsync(3, "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1").ConfigureAwait(false);

        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
        Assert.That(joiningPlayer.LoginResultOverride, Is.Null);
    }

    /// <summary>
    /// Tests that players with a different IP address don't count toward the connection limit.
    /// </summary>
    [Test]
    public async ValueTask DifferentIpDoesNotCountAsync()
    {
        await this.AddAuthenticatedPlayersAsync(3, "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("192.168.1.100").ConfigureAwait(false);

        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that players which are still at the login screen don't count toward the connection limit.
    /// </summary>
    [Test]
    public async ValueTask LoginScreenAndInitialPlayersDoNotCountAsync()
    {
        await this.AddAuthenticatedPlayersAsync(2, "127.0.0.1").ConfigureAwait(false);
        var loginScreenPlayer = new TestPlayer(this._gameContext);
        loginScreenPlayer.SetIpAddress("127.0.0.1");
        await loginScreenPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(loginScreenPlayer).ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1").ConfigureAwait(false);

        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Verifies that observe-only mode never rejects a login.
    /// </summary>
    [Test]
    public void ShouldReject_ObserveOnly_ReturnsFalse()
        => Assert.That(MaximumConnectionsPerIpPlugIn.ShouldReject(5, 3, true), Is.False);

    /// <summary>
    /// Verifies that the next connection at the configured limit is rejected.
    /// </summary>
    [Test]
    public void ShouldReject_AtLimit_ReturnsTrue()
        => Assert.That(MaximumConnectionsPerIpPlugIn.ShouldReject(3, 3, false), Is.True);

    private async ValueTask AddAuthenticatedPlayersAsync(int count, string ipAddress)
    {
        for (int i = 0; i < count; i++)
        {
            var player = new TestPlayer(this._gameContext);
            player.SetIpAddress(ipAddress);
            await player.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
            await player.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
            await this._gameContext.AddPlayerAsync(player).ConfigureAwait(false);
        }
    }

    private async ValueTask<(TestPlayer Player, StateMachine.StateChangeEventArgs EventArgs)> CreateJoiningPlayerAsync(string ipAddress)
    {
        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress(ipAddress);
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        return (joiningPlayer, new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated });
    }

    private class TestPlayer : Player, IHasIpAddress
    {
        private string? _ipAddress;

        public TestPlayer(IGameContext gameContext)
            : base(gameContext)
        {
        }

        /// <inheritdoc />
        public string? IpAddress => this._ipAddress;

        /// <summary>Sets the IP address for testing purposes.</summary>
        public void SetIpAddress(string? ip) => this._ipAddress = ip;

        protected override ICustomPlugInContainer<GameLogic.Views.IViewPlugIn> CreateViewPlugInContainer()
        {
            return new MockViewPlugInContainer();
        }
    }
}
