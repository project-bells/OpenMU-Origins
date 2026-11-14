// <copyright file="WebAccountService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Provides player-facing authentication, profile queries, self-service password updates,
/// and admin operator verification for the external website BFF.
/// </summary>
public sealed class WebAccountService
{
    private static readonly Regex PasswordAsciiPattern = new(@"^[\x20-\x7E]+$", RegexOptions.Compiled);

    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;
    private readonly ILoginServer _loginServer;
    private readonly UserManager<AdminUser>? _adminUserManager;
    private readonly ILogger<WebAccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAccountService"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration data source.</param>
    /// <param name="loginServer">The login server.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="adminUserManager">Optional AdminPanel user manager for GM authentication.</param>
    public WebAccountService(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource,
        ILoginServer loginServer,
        ILogger<WebAccountService> logger,
        UserManager<AdminUser>? adminUserManager = null)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
        this._loginServer = loginServer;
        this._logger = logger;
        this._adminUserManager = adminUserManager;
    }

    /// <summary>
    /// Verifies player account credentials and returns a safe profile without credential hashes or pins.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The plain-text password over TLS/internal channel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication status and profile when successful.</returns>
    public async ValueTask<(WebAccountAuthStatus Status, WebAccountProfile? Profile)> AuthenticatePlayerAsync(
        string loginName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedLogin = (loginName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedLogin) || string.IsNullOrEmpty(password))
        {
            return (WebAccountAuthStatus.InvalidCredentials, null);
        }

        var config = await this._gameConfigurationSource.GetOwnerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(config);
        var account = await context.GetAccountByLoginNameAsync(normalizedLogin, password, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            this._logger.LogWarning("Rejected web player login attempt for '{LoginName}'.", normalizedLogin);
            return (WebAccountAuthStatus.InvalidCredentials, null);
        }

        if (account.State is AccountState.Banned or AccountState.TemporarilyBanned)
        {
            this._logger.LogWarning("Rejected banned web player login for '{LoginName}' (State: {State}).", account.LoginName, account.State);
            return (WebAccountAuthStatus.Banned, null);
        }

        var profile = await this.BuildProfileAsync(context, account, cancellationToken).ConfigureAwait(false);
        return (WebAccountAuthStatus.Success, profile);
    }

    /// <summary>
    /// Loads the safe account profile, online state, cash shop pending coins, and characters for an account.
    /// </summary>
    /// <param name="loginName">The exact login name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The profile if found; otherwise, <c>null</c>.</returns>
    public async ValueTask<WebAccountProfile?> GetProfileAsync(string loginName, CancellationToken cancellationToken = default)
    {
        var normalizedLogin = (loginName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedLogin))
        {
            return null;
        }

        var config = await this._gameConfigurationSource.GetOwnerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(config);
        var account = await context.GetAccountByLoginNameAsync(normalizedLogin, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return null;
        }

        return await this.BuildProfileAsync(context, account, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies the current password and updates the password while the account is offline.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="currentPassword">The existing password to verify.</param>
    /// <param name="newPassword">The replacement password matching game limits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome status and descriptive error message if any.</returns>
    public async ValueTask<(WebAccountAuthStatus Status, string? ErrorMessage)> ChangePasswordAsync(
        string loginName,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var normalizedLogin = (loginName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedLogin))
        {
            return (WebAccountAuthStatus.ValidationError, "LoginName is required.");
        }

        if (string.IsNullOrEmpty(currentPassword))
        {
            return (WebAccountAuthStatus.InvalidCredentials, "Current password is required.");
        }

        if (string.IsNullOrEmpty(newPassword)
            || newPassword.Length < AccountRegistration.MinimumLength
            || newPassword.Length > AccountRegistration.PasswordMaximumLength
            || !PasswordAsciiPattern.IsMatch(newPassword))
        {
            return (
                WebAccountAuthStatus.ValidationError,
                $"Password must be {AccountRegistration.MinimumLength}-{AccountRegistration.PasswordMaximumLength} printable ASCII characters.");
        }

        var config = await this._gameConfigurationSource.GetOwnerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        using (var probeContext = this._persistenceContextProvider.CreateNewPlayerContext(config))
        {
            var probeAccount = await probeContext.GetAccountByLoginNameAsync(normalizedLogin, cancellationToken).ConfigureAwait(false);
            if (probeAccount is null)
            {
                return (WebAccountAuthStatus.AccountNotFound, "Account not found.");
            }

            if (probeAccount.State is AccountState.Banned or AccountState.TemporarilyBanned)
            {
                return (WebAccountAuthStatus.Banned, "Banned accounts cannot change password.");
            }

            if (!VerifyPasswordHash(currentPassword, probeAccount.PasswordHash))
            {
                return (WebAccountAuthStatus.InvalidCredentials, "Current password is incorrect.");
            }
        }

        var reservationId = WebCharacterMapper.CreateReservationId();
        if (!await this._loginServer.TryReserveAccountAsync(normalizedLogin, reservationId).ConfigureAwait(false))
        {
            return (WebAccountAuthStatus.AccountOnline, "Account must be offline before changing password.");
        }

        try
        {
            using var editContext = this._persistenceContextProvider.CreateNewPlayerContext(config);
            var account = await editContext.GetAccountByLoginNameAsync(normalizedLogin, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                return (WebAccountAuthStatus.AccountNotFound, "Account not found.");
            }

            if (account.State is AccountState.Banned or AccountState.TemporarilyBanned)
            {
                return (WebAccountAuthStatus.Banned, "Banned accounts cannot change password.");
            }

            if (!VerifyPasswordHash(currentPassword, account.PasswordHash))
            {
                return (WebAccountAuthStatus.InvalidCredentials, "Current password is incorrect.");
            }

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await editContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Web self-service password change completed for account '{LoginName}'.", account.LoginName);
            return (WebAccountAuthStatus.Success, null);
        }
        finally
        {
            await this._loginServer.ReleaseAccountReservationAsync(normalizedLogin, reservationId).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Verifies an AdminPanel operator account's credentials (and TOTP second factor when enabled).
    /// </summary>
    /// <param name="loginName">The admin login name.</param>
    /// <param name="password">The admin password.</param>
    /// <param name="twoFactorCode">Optional authenticator or recovery code.</param>
    /// <param name="isRecoveryCode">Whether <paramref name="twoFactorCode"/> is a recovery code.</param>
    /// <returns>The admin verification result.</returns>
    public async Task<WebAdminAuthResult> AuthenticateAdminAsync(
        string loginName,
        string password,
        string? twoFactorCode = null,
        bool isRecoveryCode = false)
    {
        if (this._adminUserManager is null)
        {
            return new WebAdminAuthResult(WebAdminAuthStatus.Unavailable, null, Array.Empty<string>());
        }

        var normalizedLogin = (loginName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedLogin) || string.IsNullOrEmpty(password))
        {
            return new WebAdminAuthResult(WebAdminAuthStatus.InvalidCredentials, null, Array.Empty<string>());
        }

        var user = await this._adminUserManager.FindByNameAsync(normalizedLogin).ConfigureAwait(false);
        if (user is null || user.IsDisabled)
        {
            return new WebAdminAuthResult(WebAdminAuthStatus.InvalidCredentials, null, Array.Empty<string>());
        }

        if (await this._adminUserManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            return new WebAdminAuthResult(WebAdminAuthStatus.LockedOut, null, Array.Empty<string>());
        }

        if (!await this._adminUserManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
        {
            await this._adminUserManager.AccessFailedAsync(user).ConfigureAwait(false);
            var lockedOut = await this._adminUserManager.IsLockedOutAsync(user).ConfigureAwait(false);
            return new WebAdminAuthResult(
                lockedOut ? WebAdminAuthStatus.LockedOut : WebAdminAuthStatus.InvalidCredentials,
                null,
                Array.Empty<string>());
        }

        if (user.IsTwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                return new WebAdminAuthResult(WebAdminAuthStatus.TwoFactorRequired, user.LoginName, Array.Empty<string>());
            }

            var code = twoFactorCode.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            var secondFactorValid = isRecoveryCode
                ? (await this._adminUserManager.RedeemTwoFactorRecoveryCodeAsync(user, code).ConfigureAwait(false)).Succeeded
                : await this._adminUserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code).ConfigureAwait(false);
            if (!secondFactorValid)
            {
                await this._adminUserManager.AccessFailedAsync(user).ConfigureAwait(false);
                var lockedOut = await this._adminUserManager.IsLockedOutAsync(user).ConfigureAwait(false);
                return new WebAdminAuthResult(
                    lockedOut ? WebAdminAuthStatus.LockedOut : WebAdminAuthStatus.InvalidCredentials,
                    null,
                    Array.Empty<string>());
            }
        }

        await this._adminUserManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        user.LastLoginAt = DateTime.UtcNow;
        await this._adminUserManager.UpdateAsync(user).ConfigureAwait(false);

        var assignedRoles = (user.Roles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var effectiveRoles = assignedRoles
            .SelectMany(AdminRoles.GetEffectiveRoles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new WebAdminAuthResult(WebAdminAuthStatus.Success, user.LoginName, effectiveRoles);
    }

    private static bool VerifyPasswordHash(string plainText, string passwordHash)
    {
        try
        {
            return !string.IsNullOrEmpty(passwordHash) && BCrypt.Net.BCrypt.Verify(plainText, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private async ValueTask<WebAccountProfile> BuildProfileAsync(
        IPlayerContext context,
        Account account,
        CancellationToken cancellationToken)
    {
        var onlineSnapshot = await this._loginServer.GetSnapshotAsync().ConfigureAwait(false);
        var isOnline = onlineSnapshot.TryGetValue(account.LoginName, out var serverId);

        var pendingGrants = await context.GetPendingCashShopCoinGrantsAsync(account.GetId(), cancellationToken).ConfigureAwait(false);
        var pendingWCoinC = (int)Math.Clamp(pendingGrants.Where(g => g.CoinType == CashShopCoinType.WCoinC).Sum(g => (long)g.Amount), int.MinValue, int.MaxValue);
        var pendingWCoinP = (int)Math.Clamp(pendingGrants.Where(g => g.CoinType == CashShopCoinType.WCoinP).Sum(g => (long)g.Amount), int.MinValue, int.MaxValue);
        var pendingGoblin = (int)Math.Clamp(pendingGrants.Where(g => g.CoinType == CashShopCoinType.GoblinPoints).Sum(g => (long)g.Amount), int.MinValue, int.MaxValue);

        var characters = account.Characters
            .OrderBy(c => c.CharacterSlot)
            .Select(WebCharacterMapper.ToWebCharacterInfo)
            .ToArray();

        return new WebAccountProfile(
            account.GetId(),
            account.LoginName,
            account.EMail,
            account.State.ToString(),
            account.RegistrationDate,
            account.VipUntil,
            account.OnlineTimeSeconds,
            isOnline,
            isOnline ? serverId : null,
            new WebAccountBalanceInfo(
                account.WCoinC,
                account.WCoinP,
                account.GoblinPoints,
                pendingWCoinC,
                pendingWCoinP,
                pendingGoblin),
            characters);
    }
}

