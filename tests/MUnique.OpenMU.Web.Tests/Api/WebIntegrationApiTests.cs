// <copyright file="WebIntegrationApiTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Web.AdminPanel.API;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Operations;
using MUnique.OpenMU.Web.AdminPanel.Services;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Tests.AdminAuth;
using MUnique.OpenMU.Web.Tests.Mcp;

/// <summary>
/// End-to-end HTTP integration tests for player authentication, search, profile, admin verification,
/// offline character self-service, and cash shop pending totals exposed to OpenMU-Web.
/// </summary>
[TestFixture]
public sealed class WebIntegrationApiTests : IDisposable
{
    private const string ViewerKey = "omu_web-integration-viewer-key";
    private const string OperatorKey = "omu_web-integration-operator-key";
    private const string AdminKey = "omu_web-integration-admin-key";

    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private InMemoryPersistenceContextProvider _persistence = null!;
    private GameConfiguration _config = null!;
    private Dictionary<string, byte> _onlineAccounts = null!;
    private HashSet<string> _reservedAccounts = null!;

    /// <summary>
    /// Starts an isolated ASP.NET Core admin API host backed by in-memory repositories.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._onlineAccounts = new Dictionary<string, byte>(StringComparer.Ordinal);
        this._reservedAccounts = new HashSet<string>(StringComparer.Ordinal);
        this._persistence = new InMemoryPersistenceContextProvider();

        using (var context = this._persistence.CreateNewContext())
        {
            var config = context.CreateNew<GameConfiguration>();
            config.MaximumLevel = 400;
            config.ExperienceFormula = "10 * level * level";

            var levelDef = context.CreateNew<AttributeDefinition>(Stats.Level.Id, Stats.Level.Designation, Stats.Level.Description);
            var resetsDef = context.CreateNew<AttributeDefinition>(Stats.Resets.Id, Stats.Resets.Designation, Stats.Resets.Description);
            var strDef = context.CreateNew<AttributeDefinition>(Stats.BaseStrength.Id, Stats.BaseStrength.Designation, Stats.BaseStrength.Description);
            var agiDef = context.CreateNew<AttributeDefinition>(Stats.BaseAgility.Id, Stats.BaseAgility.Designation, Stats.BaseAgility.Description);
            var vitDef = context.CreateNew<AttributeDefinition>(Stats.BaseVitality.Id, Stats.BaseVitality.Designation, Stats.BaseVitality.Description);
            var eneDef = context.CreateNew<AttributeDefinition>(Stats.BaseEnergy.Id, Stats.BaseEnergy.Designation, Stats.BaseEnergy.Description);
            config.Attributes.Add(levelDef);
            config.Attributes.Add(resetsDef);
            config.Attributes.Add(strDef);
            config.Attributes.Add(agiDef);
            config.Attributes.Add(vitDef);
            config.Attributes.Add(eneDef);

            var lorencia = context.CreateNew<GameMapDefinition>();
            lorencia.Number = 0;
            lorencia.Name = "Lorencia";
            var spawn = context.CreateNew<ExitGate>();
            spawn.IsSpawnGate = true;
            spawn.Map = lorencia;
            spawn.X1 = 125;
            spawn.X2 = 135;
            spawn.Y1 = 125;
            spawn.Y2 = 135;
            lorencia.ExitGates.Add(spawn);
            config.Maps.Add(lorencia);

            var dkClass = context.CreateNew<CharacterClass>();
            dkClass.Number = 16;
            dkClass.Name = "Dark Knight";
            dkClass.HomeMap = lorencia;
            dkClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(strDef, 28f, true));
            dkClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(agiDef, 20f, true));
            dkClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(vitDef, 25f, true));
            dkClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(eneDef, 10f, true));
            config.CharacterClasses.Add(dkClass);

            var resetPluginConfig = context.CreateNew<PlugInConfiguration>();
            resetPluginConfig.TypeId = typeof(ResetFeaturePlugIn).GUID;
            resetPluginConfig.IsActive = true;
            resetPluginConfig.SetConfiguration(new ResetConfiguration
            {
                RequiredLevel = 400,
                LevelAfterReset = 10,
                RequiredMoney = 1000000,
                MultiplyRequiredMoneyByResetCount = true,
                ResetStats = true,
                PointsPerReset = 1500,
                MultiplyPointsByResetCount = true,
                ReplacePointsPerReset = true,
                MoveHome = true,
            }, null);
            config.PlugInConfigurations.Add(resetPluginConfig);

            var statResetConfig = context.CreateNew<PlugInConfiguration>();
            statResetConfig.TypeId = typeof(StatResetFeaturePlugIn).GUID;
            statResetConfig.IsActive = true;
            statResetConfig.SetConfiguration(new StatResetConfiguration
            {
                RequiredLevel = 1,
                RequiredMoney = 500000,
                MoveHome = false,
            }, null);
            config.PlugInConfigurations.Add(statResetConfig);

            var pkClearConfig = context.CreateNew<PlugInConfiguration>();
            pkClearConfig.TypeId = typeof(PkClearChatCommandPlugIn).GUID;
            pkClearConfig.IsActive = true;
            pkClearConfig.SetConfiguration(new PkClearChatCommandPlugIn.PKClearConfiguration
            {
                ZenCostPerKill = 1000000,
                AllowRegularPlayers = true,
            }, null);
            config.PlugInConfigurations.Add(pkClearConfig);

            await context.SaveChangesAsync().ConfigureAwait(false);
            this._config = config;
        }

        using (var playerContext = this._persistence.CreateNewPlayerContext(this._config))
        {
            var account = playerContext.CreateNew<Account>();
            account.LoginName = "hero01";
            account.EMail = "hero01@example.org";
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPass123");
            account.SecurityCode = "123456";
            account.VaultPassword = "9999";
            account.OnlineTimeSeconds = 7500;
            account.WCoinC = 120;

            var dkClass = this._config.CharacterClasses.First();
            var character = playerContext.CreateNew<Character>();
            character.Name = "BladeOne";
            character.CharacterClass = dkClass;
            character.CurrentMap = this._config.Maps.First();
            character.LevelUpPoints = 250;
            character.PlayerKillCount = 2;
            character.State = HeroState.PlayerKiller1stStage;
            character.Inventory = playerContext.CreateNew<ItemStorage>();
            character.Inventory.Money = 50_000_000;
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.Level.Id), 400f));
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.Resets.Id), 1f));
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.BaseStrength.Id), 128f));
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.BaseAgility.Id), 70f));
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.BaseVitality.Id), 25f));
            character.Attributes.Add(playerContext.CreateNew<StatAttribute>(this._config.Attributes.First(a => a.Id == Stats.BaseEnergy.Id), 10f));
            account.Characters.Add(character);

            var banned = playerContext.CreateNew<Account>();
            banned.LoginName = "banned01";
            banned.EMail = "banned@example.org";
            banned.PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPass123");
            banned.State = AccountState.Banned;

            for (var i = 0; i < 60; i++)
            {
                var pendingGrant = playerContext.CreateNew<CashShopCoinGrant>();
                pendingGrant.AccountId = account.GetId();
                pendingGrant.CoinType = CashShopCoinType.WCoinC;
                pendingGrant.Amount = 10;
                pendingGrant.Reason = $"Batch grant {i}";
                pendingGrant.Reference = $"batch-{i}";
                pendingGrant.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            }

            await playerContext.SaveChangesAsync().ConfigureAwait(false);
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminPanel:Auth:DataProtectionKeyPath"] = Path.Combine(Path.GetTempPath(), "openmu-web-integration-keys"),
        });

        var keys = new InMemoryApiKeyRepository();
        await keys.AddAsync(new ApiKey { Id = Guid.NewGuid(), Name = "viewer", KeyHash = ApiKeyGenerator.Hash(ViewerKey), Roles = AdminRoles.Viewer }).ConfigureAwait(false);
        await keys.AddAsync(new ApiKey { Id = Guid.NewGuid(), Name = "operator", KeyHash = ApiKeyGenerator.Hash(OperatorKey), Roles = AdminRoles.Operator }).ConfigureAwait(false);
        await keys.AddAsync(new ApiKey { Id = Guid.NewGuid(), Name = "admin", KeyHash = ApiKeyGenerator.Hash(AdminKey), Roles = AdminRoles.Administrator }).ConfigureAwait(false);

        var adminUsers = new InMemoryAdminUserRepository();
        var gmUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            LoginName = "gmchief",
            NormalizedLoginName = "GMCHIEF",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("GmSuperSecret123"),
            Roles = AdminRoles.Administrator,
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };
        await adminUsers.AddAsync(gmUser).ConfigureAwait(false);

        builder.Services.AddSingleton<IApiKeyRepository>(keys);
        builder.Services.AddSingleton<IAdminUserRepository>(adminUsers);
        builder.Services.AddSingleton<IPersistenceContextProvider>(this._persistence);
        var configSource = new Mock<IDataSource<GameConfiguration>>();
        configSource.Setup(c => c.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(this._config);
        builder.Services.AddSingleton(configSource.Object);

        var loginServer = new Mock<ILoginServer>();
        loginServer.Setup(l => l.GetSnapshotAsync()).ReturnsAsync(() => new Dictionary<string, byte>(this._onlineAccounts));
        loginServer.Setup(l => l.TryReserveAccountAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((name, reservationId) =>
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(reservationId, byte.MaxValue);
                if (this._onlineAccounts.ContainsKey(name) || !this._reservedAccounts.Add(name))
                {
                    return ValueTask.FromResult(false);
                }

                return ValueTask.FromResult(true);
            });
        loginServer.Setup(l => l.ReleaseAccountReservationAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((name, _) =>
            {
                this._reservedAccounts.Remove(name);
                return ValueTask.CompletedTask;
            });
        builder.Services.AddSingleton(loginServer.Object);
        builder.Services.AddSingleton(Mock.Of<IGuildServer>());
        var servers = new Mock<IServerProvider>();
        servers.SetupGet(s => s.Servers).Returns(new List<IManageableServer>());
        builder.Services.AddSingleton(servers.Object);
        builder.Services.AddSingleton(Mock.Of<IModalService>());
        builder.Services.AddSingleton(Mock.Of<IToastService>());
        builder.Services.AddSingleton<IAdminAuditRepository, InMemoryAdminAuditRepository>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAdminPanelAuth(builder.Configuration);
        builder.Services.AddSingleton<MUnique.OpenMU.Web.AdminPanel.Operations.AdminWriteSettings>();
        builder.Services.AddSingleton<MUnique.OpenMU.Web.AdminPanel.Operations.AdminAuditSettings>();
        builder.Services.AddScoped<MUnique.OpenMU.Web.AdminPanel.Operations.AdminOperationExecutor>();
        builder.Services.AddScoped<MUnique.OpenMU.Web.AdminPanel.Operations.AccountReadOperations>();
        builder.Services.AddScoped<MUnique.OpenMU.Web.AdminPanel.Operations.AccountWriteOperations>();
        builder.Services.AddScoped<WebAccountService>();
        builder.Services.AddScoped<WebCharacterSelfService>();
        builder.Services.AddControllers(options => options.Filters.Add<AdminOperationExceptionFilter>())
            .AddApplicationPart(typeof(AccountController).Assembly);

        this._app = builder.Build();
        this._app.UseRouting();
        this._app.UseAdminPanelAuth();
        this._app.MapControllers();
        await this._app.StartAsync().ConfigureAwait(false);
        this._http = new HttpClient { BaseAddress = new Uri(this._app.Urls.Single()) };
    }

    /// <summary>
    /// Disposes the HTTP test host.
    /// </summary>
    [TearDown]
    public async Task TearDownAsync()
    {
        this.Dispose();
        if (this._app is not null)
        {
            await this._app.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._http?.Dispose();
    }

    /// <summary>
    /// Verifies player authentication, banned account rejection, search, and pending coin summation.
    /// </summary>
    [Test]
    public async Task PlayerAuthenticateProfileAndSearchWorkWithoutLeakingSecretsAsync()
    {
        using var unauthReq = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/authenticate")
        {
            Content = JsonContent.Create(new { loginName = "hero01", password = "StrongPass123" }),
        };
        using var unauthResp = await this._http.SendAsync(unauthReq).ConfigureAwait(false);
        Assert.That(unauthResp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using var wrongPassReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/authenticate", new { loginName = "hero01", password = "WrongPassword" });
        using var wrongPassResp = await this._http.SendAsync(wrongPassReq).ConfigureAwait(false);
        Assert.That(wrongPassResp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using var bannedReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/authenticate", new { loginName = "banned01", password = "StrongPass123" });
        using var bannedResp = await this._http.SendAsync(bannedReq).ConfigureAwait(false);
        Assert.That(bannedResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        using var okReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/authenticate", new { loginName = "hero01", password = "StrongPass123" });
        using var okResp = await this._http.SendAsync(okReq).ConfigureAwait(false);
        Assert.That(okResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var payload = await okResp.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.That(payload, Does.Not.Contain("PasswordHash"));
        Assert.That(payload, Does.Not.Contain("123456"));
        Assert.That(payload, Does.Not.Contain("9999"));

        var json = JsonNode.Parse(payload)!.AsObject();
        Assert.That(json["onlineTimeSeconds"]!.GetValue<long>(), Is.EqualTo(7500));
        Assert.That(json["balance"]!["wCoinC"]!.GetValue<int>(), Is.EqualTo(120));
        Assert.That(json["balance"]!["pendingWCoinC"]!.GetValue<int>(), Is.EqualTo(600));
        Assert.That(json["characters"]!.AsArray().Count, Is.EqualTo(1));

        using var searchReq = this.WithKey(ViewerKey, HttpMethod.Get, "/api/accounts?text=hero01");
        using var searchResp = await this._http.SendAsync(searchReq).ConfigureAwait(false);
        Assert.That(searchResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var searchJson = JsonNode.Parse(await searchResp.Content.ReadAsStringAsync().ConfigureAwait(false))!.AsObject();
        Assert.That(searchJson["total"]!.GetValue<int>(), Is.GreaterThanOrEqualTo(1));
    }

    /// <summary>
    /// Verifies password change requires correct current password and enforces offline locking.
    /// </summary>
    [Test]
    public async Task ChangePasswordEnforcesCurrentPasswordAndOfflineReservationAsync()
    {
        this._onlineAccounts["hero01"] = 1;
        using var onlineReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/password/change", new
        {
            currentPassword = "StrongPass123",
            newPassword = "NewPassword99",
        });
        using var onlineResp = await this._http.SendAsync(onlineReq).ConfigureAwait(false);
        Assert.That(onlineResp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        this._onlineAccounts.Clear();
        using var badCurReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/password/change", new
        {
            currentPassword = "Wrong",
            newPassword = "NewPassword99",
        });
        using var badCurResp = await this._http.SendAsync(badCurReq).ConfigureAwait(false);
        Assert.That(badCurResp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using var changeReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/password/change", new
        {
            currentPassword = "StrongPass123",
            newPassword = "NewPassword99",
        });
        using var changeResp = await this._http.SendAsync(changeReq).ConfigureAwait(false);
        Assert.That(changeResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var reAuthReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/authenticate", new
        {
            loginName = "hero01",
            password = "NewPassword99",
        });
        using var reAuthResp = await this._http.SendAsync(reAuthReq).ConfigureAwait(false);
        Assert.That(reAuthResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Verifies AdminPanel operator credential verification endpoint.
    /// </summary>
    [Test]
    public async Task AdminAuthenticateVerifiesPasswordAndReturnsRolesAsync()
    {
        using var badReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/admin/auth/verify", new
        {
            loginName = "gmchief",
            password = "WrongPassword",
        });
        using var badResp = await this._http.SendAsync(badReq).ConfigureAwait(false);
        Assert.That(badResp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using var okReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/admin/auth/verify", new
        {
            loginName = "gmchief",
            password = "GmSuperSecret123",
        });
        using var okResp = await this._http.SendAsync(okReq).ConfigureAwait(false);
        Assert.That(okResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = JsonNode.Parse(await okResp.Content.ReadAsStringAsync().ConfigureAwait(false))!.AsObject();
        Assert.That(json["authenticated"]!.GetValue<bool>(), Is.True);
        Assert.That(json["roles"]!.AsArray().Select(n => n!.GetValue<string>()), Does.Contain(AdminRoles.Administrator));
    }

    /// <summary>
    /// Verifies offline PK clear, stat allocation, stat reset, and character reset enforce game rules.
    /// </summary>
    [Test]
    public async Task OfflineCharacterSelfServiceOperationsEnforceGameRulesAsync()
    {
        using var addBadStat = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/add-stats", new
        {
            strength = 0,
            agility = 0,
            vitality = 0,
            energy = 0,
            leadership = 10,
        });
        using var addBadResp = await this._http.SendAsync(addBadStat).ConfigureAwait(false);
        Assert.That(addBadResp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        using var addOk = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/add-stats", new
        {
            strength = 50,
            agility = 25,
            vitality = 0,
            energy = 0,
            leadership = 0,
        });
        using var addOkResp = await this._http.SendAsync(addOk).ConfigureAwait(false);
        Assert.That(addOkResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var afterAdd = JsonNode.Parse(await addOkResp.Content.ReadAsStringAsync().ConfigureAwait(false))!["character"]!;
        Assert.That(afterAdd["levelUpPoints"]!.GetValue<int>(), Is.EqualTo(175));
        Assert.That(afterAdd["stats"]!["strength"]!.GetValue<int>(), Is.EqualTo(178));

        using var pkClearReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/pk-clear", new { });
        using var pkClearResp = await this._http.SendAsync(pkClearReq).ConfigureAwait(false);
        Assert.That(pkClearResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var afterPk = JsonNode.Parse(await pkClearResp.Content.ReadAsStringAsync().ConfigureAwait(false))!;
        Assert.That(afterPk["zenSpent"]!.GetValue<int>(), Is.EqualTo(2_000_000));
        Assert.That(afterPk["character"]!["playerKillCount"]!.GetValue<int>(), Is.EqualTo(0));

        using var statResetReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/reset-stats", new { });
        using var statResetResp = await this._http.SendAsync(statResetReq).ConfigureAwait(false);
        Assert.That(statResetResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var afterStatReset = JsonNode.Parse(await statResetResp.Content.ReadAsStringAsync().ConfigureAwait(false))!;
        Assert.That(afterStatReset["character"]!["stats"]!["strength"]!.GetValue<int>(), Is.EqualTo(28));
        Assert.That(afterStatReset["character"]!["levelUpPoints"]!.GetValue<int>(), Is.GreaterThan(175));

        using var resetReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/reset", new { });
        using var resetResp = await this._http.SendAsync(resetReq).ConfigureAwait(false);
        Assert.That(resetResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var afterReset = JsonNode.Parse(await resetResp.Content.ReadAsStringAsync().ConfigureAwait(false))!;
        Assert.That(afterReset["character"]!["resets"]!.GetValue<int>(), Is.EqualTo(2));
        Assert.That(afterReset["character"]!["level"]!.GetValue<int>(), Is.EqualTo(10));
        Assert.That(afterReset["character"]!["levelUpPoints"]!.GetValue<int>(), Is.EqualTo(3000));

        // Second reset immediately fails because level is now 10 (< 400).
        using var secondResetReq = this.WithKey(OperatorKey, HttpMethod.Post, "/api/accounts/hero01/characters/BladeOne/reset", new { });
        using var secondResetResp = await this._http.SendAsync(secondResetReq).ConfigureAwait(false);
        Assert.That(secondResetResp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private HttpRequestMessage WithKey(string key, HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add(ApiKeyAuthenticationDefaults.HeaderName, key);
        if (body is not null)
        {
            req.Content = JsonContent.Create(body);
        }

        return req;
    }
}
