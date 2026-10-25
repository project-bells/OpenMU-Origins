// <copyright file="PacketAdmissionGuard.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// Applies a token bucket at the first decoded-packet boundary of one connection.
/// </summary>
internal sealed class PacketAdmissionGuard
{
    private readonly object _sync = new();
    private readonly ServerAuthoritativeAntiCheatPlugIn _antiCheat;
    private readonly Player _player;
    private double _tokens;
    private DateTimeOffset? _lastRefill;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketAdmissionGuard"/> class.
    /// </summary>
    /// <param name="player">The player represented by the connection.</param>
    /// <param name="antiCheat">The central anti-cheat plugin.</param>
    public PacketAdmissionGuard(Player player, ServerAuthoritativeAntiCheatPlugIn antiCheat)
    {
        this._player = player;
        this._antiCheat = antiCheat;
        this._tokens = antiCheat.Configuration?.PacketBurstCapacity ?? 0;
    }

    /// <summary>
    /// Tests whether a packet is admitted and returns a decision when the rate is exceeded.
    /// </summary>
    /// <param name="now">The server timestamp.</param>
    /// <param name="packetLength">The decoded packet length, used only as an audit fact.</param>
    /// <param name="decision">The risk decision for an exceeded budget.</param>
    /// <returns><c>true</c> when processing may continue; otherwise, <c>false</c>.</returns>
    public bool TryAdmit(DateTimeOffset now, int packetLength, out AntiCheatDecision? decision)
    {
        lock (this._sync)
        {
            if (this._antiCheat.Configuration is not { } configuration)
            {
                decision = null;
                return true;
            }

            if (this._lastRefill is { } previous)
            {
                var elapsedSeconds = Math.Max(0, (now - previous).TotalSeconds);
                this._tokens = Math.Min(
                    configuration.PacketBurstCapacity,
                    this._tokens + (elapsedSeconds * configuration.PacketsPerSecond));
            }

            this._lastRefill = now;
            if (this._tokens >= 1)
            {
                this._tokens--;
                decision = null;
                return true;
            }

            var evidence = new AntiCheatEvidence(
                AntiCheatCategory.PacketFrequency,
                configuration.PacketFloodRiskPoints,
                "packet.frequency.exceeded",
                $"length={packetLength}",
                now);
            decision = this._antiCheat.Report(this._player, evidence, true);
            return configuration.ObserveOnly;
        }
    }
}
