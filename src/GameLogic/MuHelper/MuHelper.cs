// <copyright file="MuHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MuHelper;

using System.Diagnostics;
using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views.MuHelper;
using Nito.AsyncEx;

/// <summary>
/// Implements the logic of the 'MU Helper'.
/// </summary>
public class MuHelper : AsyncDisposable
{
    /// <summary>
    /// The <see cref="IElement"/> which is added to the players <see cref="IAttributeSystem"/>
    /// when the MU Helper is active.
    /// </summary>
    private static readonly IElement ActiveElement = new ConstantElement(1);

    /// <summary>
    /// Associated player.
    /// </summary>
    private readonly Player _player;

    /// <summary>
    /// The current configuration.
    /// </summary>
    private readonly MuHelperConfiguration _configuration;

    private readonly AsyncLock _lifecycleLock = new();

    private CancellationTokenSource? _stopCts;
    private Task? _runTask;
    private DateTime _startTimestamp;
    private bool _isStopping;

    /// <summary>
    /// Initializes a new instance of the <see cref="MuHelper"/> class.
    /// </summary>
    /// <param name="player">current player.</param>
    public MuHelper(Player player)
    {
        this._player = player;
        this._configuration = this._player.GameContext.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()?.Configuration ?? new MuHelperConfiguration();
    }

    /// <summary>
    /// Start Mu Helper.
    /// </summary>
    public async ValueTask<bool> TryStartAsync()
    {
        using var lifecycleLock = await this._lifecycleLock.LockAsync().ConfigureAwait(false);
        if (this._runTask is not null || this._isStopping)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperAlreadyRunning)).ConfigureAwait(false);
            return false;
        }

        if (this._player.Level < this._configuration.MinLevel)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperMinimumLevel), this._configuration.MinLevel).ConfigureAwait(false);
            return false;
        }

        if (this._player.Level > this._configuration.MaxLevel)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperMaximumLevel), this._configuration.MaxLevel).ConfigureAwait(false);
            return false;
        }

        this._startTimestamp = DateTime.UtcNow;
        var requiredMoney = this.CalculateRequiredMoney();

        if (!this._player.TryRemoveMoney(requiredMoney))
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperRequiresMoney), requiredMoney).ConfigureAwait(false);
            return false;
        }

        await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.StartAsync()).ConfigureAwait(false);
        await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.ConsumeMoneyAsync((uint)requiredMoney)).ConfigureAwait(false);

        this._player.Attributes?.AddElement(ActiveElement, Stats.IsMuHelperActive);
        this._stopCts?.Dispose();
        this._stopCts = new CancellationTokenSource();
        var cts = this._stopCts.Token;
        this._runTask = this.RunLoopAsync(cts);
        return true;
    }

    /// <summary>
    /// Stops the MU Helper.
    /// </summary>
    public async ValueTask StopAsync()
    {
        await this.StopAsync(true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await this.StopAsync().ConfigureAwait(false);
    }

    private async ValueTask StopAsync(bool waitForRunTask)
    {
        Task runTask;
        CancellationTokenSource stopCts;
        using (await this._lifecycleLock.LockAsync().ConfigureAwait(false))
        {
            if (this._runTask is not { } currentRunTask
                || this._stopCts is not { } currentStopCts)
            {
                return;
            }

            runTask = currentRunTask;
            stopCts = currentStopCts;
            this._isStopping = true;
            this._runTask = null;
            this._stopCts = null;
        }

        try
        {
            this._player.Attributes?.RemoveElement(ActiveElement, Stats.IsMuHelperActive);

            await stopCts.CancelAsync().ConfigureAwait(false);

            if (waitForRunTask)
            {
                await runTask.ConfigureAwait(false);
            }

            stopCts.Dispose();

            await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.StopAsync()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._player.Logger.LogWarning(ex, "Exception during stopping the mu helper for {CharacterName}: {Error}", this._player.Name, ex.Message);
        }
        finally
        {
            using var lifecycleLock = await this._lifecycleLock.LockAsync().ConfigureAwait(false);
            this._isStopping = false;
        }
    }

    private int CalculateRequiredMoney() =>
        MuHelperZenCostCalculator.Calculate(this._player, this._configuration, this._startTimestamp);

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (this._configuration.PayInterval <= TimeSpan.Zero)
            {
                this._player.Logger.LogDebug("MU Helper PayInterval is {PayInterval}. Stopping for {CharacterName}.", this._configuration.PayInterval, this._player.Name);
                return;
            }

            using var timer = new PeriodicTimer(this._configuration.PayInterval);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                await this.CollectAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when StopAsync cancels the token.
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message, ex.StackTrace);
        }
    }

    /// <summary>
    /// Performs the money collection.
    /// </summary>
    private async ValueTask CollectAsync()
    {
        var amount = this.CalculateRequiredMoney();
        if (amount > 0 && this._player.TryRemoveMoney(amount))
        {
            await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.ConsumeMoneyAsync((uint)amount)).ConfigureAwait(false);
        }
        else
        {
            // This method runs as part of _runTask. Awaiting that task while stopping
            // would deadlock, so only external callers wait for the loop to finish.
            await this.StopAsync(false).ConfigureAwait(false);
        }
    }
}
