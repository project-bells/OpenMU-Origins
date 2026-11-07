// <copyright file="ServerController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using MUnique.OpenMU.DataModel.Configuration;
    using MUnique.OpenMU.DataModel.Entities;
    using MUnique.OpenMU.GameLogic;
    using MUnique.OpenMU.GameLogic.Attributes;
    using MUnique.OpenMU.GameLogic.CastleSiege;
    using MUnique.OpenMU.GameLogic.PlugIns;
    using MUnique.OpenMU.GameServer;
    using MUnique.OpenMU.Interfaces;
    using MUnique.OpenMU.Persistence;
    using MUnique.OpenMU.Web.AdminPanel.Auth;

    /// <summary>
    /// Server API controller.
    /// </summary>
    /// <remarks>
    /// This is the public API of the server, meant to be used by external applications like a
    /// game launcher or a website. Unlike the rest of the admin panel, it accepts an API key in
    /// addition to the authentication cookie, because such an application can't go through an
    /// interactive login with a second factor.
    /// </remarks>
    [Route("api/")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Viewer)]
    public class ServerController : Controller
    {
        private static readonly Guid LevelAttributeId = new("560931AD-0901-4342-B7F4-FD2E2FCC0563");
        private static readonly Guid MasterLevelAttributeId = new("70CD8C10-391A-4C51-9AA4-A854600E3A9F");
        private static readonly Guid ResetsAttributeId = new("89A891A7-F9F9-4AB5-AF36-12056E53A5F7");

        private readonly IDictionary<int, IGameServer> _gameServers;
        private readonly IPersistenceContextProvider? _persistenceContextProvider;
        private readonly IDataSource<GameConfiguration>? _gameConfigurationSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerController"/> class.
        /// </summary>
        /// <param name="gameServers">The game servers.</param>
        /// <param name="persistenceContextProvider">The persistence context provider.</param>
        /// <param name="gameConfigurationSource">The game configuration source.</param>
        public ServerController(
            IDictionary<int, IGameServer> gameServers,
            IPersistenceContextProvider? persistenceContextProvider = null,
            IDataSource<GameConfiguration>? gameConfigurationSource = null)
        {
            this._gameServers = gameServers;
            this._persistenceContextProvider = persistenceContextProvider;
            this._gameConfigurationSource = gameConfigurationSource;
        }

        /// <summary>
        /// Sends a global message to the specified server.
        /// </summary>
        /// <param name="id">The server id.</param>
        /// <param name="msg">The message.</param>
        /// <remarks>
        /// This is the only endpoint of the API which does something instead of reporting
        /// something, so it requires the operator role and not just the viewer role.
        /// </remarks>
        [Route("send/{id=0}")]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
        public async Task<IActionResult> SendGlobalMessageAsync(int id, [FromQuery(Name = "msg")] string msg)
        {
            var server = (GameServer)this._gameServers.Values.ElementAt(id);
            if (server is not null)
            {
                await server.Context.SendGlobalNotificationAsync(msg).ConfigureAwait(false);
                return this.Ok("Done");
            }

            return this.Ok("Server not ready");
        }

        /// <summary>
        /// Gets a flag, if the specified account is currently online.
        /// </summary>
        /// <param name="accountName">Name of the account.</param>
        /// <returns>True, when online.</returns>
        [HttpGet]
        [Route("is-online/{accountName=0}")]
        public async Task<bool> GetIsOnlineAsync(string accountName)
        {
            var isOnline = false;

            foreach (var server in this._gameServers.Values.OfType<GameServer>())
            {
                var players = await server.Context.GetPlayersAsync().ConfigureAwait(false);
                if (players.Any(p => p.Account?.LoginName == accountName))
                {
                    isOnline = true;
                    break;
                }
            }

            return isOnline;
        }

        /// <summary>
        /// Gets the server state.
        /// </summary>
        [HttpGet]
        [Route("status")]
        public IActionResult ServerState()
        {
            int sum = 0;
            var list = new List<string>();
            var realms = new List<object>();

            foreach (var item in this._gameServers.Values)
            {
                if (item is GameServer server)
                {
                    var online = server.Context.PlayerCount;
                    var max = server.MaximumConnections > 0 ? server.MaximumConnections : 2500;
                    var loadPct = max > 0 ? Math.Clamp((int)Math.Round((double)online / max * 100), 0, 100) : 0;
                    var pvp = server.Context.PvpEnabled;

                    var name = !string.IsNullOrWhiteSpace(server.Description)
                        ? server.Description
                        : $"Origins-{server.Id + 1}";

                    sum += online;

                    realms.Add(new
                    {
                        id = (int)server.Id,
                        name,
                        online,
                        max,
                        loadPct,
                        pvp,
                        status = server.ServerState == MUnique.OpenMU.Interfaces.ServerState.Started ? "Normal" : server.ServerState.ToString(),
                    });
                }
            }

            TimeSpan uptime;
            try
            {
                uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            }
            catch
            {
                uptime = TimeSpan.Zero;
            }

            var uptimeText = uptime.TotalDays >= 1
                ? $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m"
                : $"{(int)uptime.TotalHours}h {uptime.Minutes}m";

            var response = new
            {
                state = "Online",
                players = sum,
                uptime = uptimeText,
                antiCheat = "Origins GameGuard 100% Active",
                playersList = list,
                realms,
            };

            return this.Ok(JsonSerializer.Serialize(response));
        }

        /// <summary>
        /// Gets real character rankings synchronized directly with the server database.
        /// </summary>
        /// <param name="search">Optional search term for character name or guild name.</param>
        /// <param name="classFilter">Optional class code filter (e.g. DK, DW, ELF, MG, DL, SUM, RF, or ALL).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ranked list of characters.</returns>
        [HttpGet]
        [Route("rankings/characters")]
        public async Task<IActionResult> GetCharacterRankingsAsync(
            [FromQuery] string? search,
            [FromQuery(Name = "class")] string? classFilter,
            CancellationToken cancellationToken)
        {
            var provider = this.GetPersistenceContextProvider();
            if (provider is null)
            {
                return this.Ok(JsonSerializer.Serialize(new { total = 0, characters = Array.Empty<object>() }));
            }

            var characters = await this.LoadAllCharactersAsync(provider, cancellationToken).ConfigureAwait(false);
            var onlineNames = await this.GetOnlineCharacterNamesAsync().ConfigureAwait(false);

            var filtered = characters.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(c =>
                    c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.GuildName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(classFilter) && !classFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.ClassShort.Equals(classFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            var sorted = filtered
                .OrderByDescending(c => c.Resets)
                .ThenByDescending(c => c.Level + c.MasterLevel)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rank = 1;
            var result = sorted.Select(c => new
            {
                rank = rank++,
                name = c.Name,
                classId = c.ClassId,
                className = c.ClassName,
                classShort = c.ClassShort,
                level = c.Level,
                masterLevel = c.MasterLevel,
                resets = c.Resets,
                grandResets = c.Resets > 0 ? (int)(c.Resets / 25) : 0,
                guild = c.GuildName,
                isOnline = onlineNames.Contains(c.Name),
            }).ToList();

            return this.Ok(JsonSerializer.Serialize(new { total = result.Count, characters = result }));
        }

        /// <summary>
        /// Gets real guild rankings synchronized directly with the server database.
        /// </summary>
        /// <param name="search">Optional search term for guild name or guild master name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ranked list of guilds.</returns>
        [HttpGet]
        [Route("rankings/guilds")]
        public async Task<IActionResult> GetGuildRankingsAsync(
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            var provider = this.GetPersistenceContextProvider();
            if (provider is null)
            {
                return this.Ok(JsonSerializer.Serialize(new { total = 0, guilds = Array.Empty<object>() }));
            }

            var characters = await this.LoadAllCharactersAsync(provider, cancellationToken).ConfigureAwait(false);
            var characterResets = characters.ToDictionary(c => c.Id, c => c.Resets);

            var guilds = await this.LoadAllGuildsAsync(provider, characterResets, cancellationToken).ConfigureAwait(false);

            var filtered = guilds.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(g =>
                    g.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    g.Master.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var sorted = filtered
                .OrderByDescending(g => g.TotalResets)
                .ThenByDescending(g => g.Score)
                .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rank = 1;
            var result = sorted.Select(g => new
            {
                rank = rank++,
                name = g.Name,
                master = g.Master,
                score = g.Score,
                totalResets = g.TotalResets,
                members = g.Members,
                siegeStatus = g.SiegeStatus,
            }).ToList();

            return this.Ok(JsonSerializer.Serialize(new { total = result.Count, guilds = result }));
        }

        private IPersistenceContextProvider? GetPersistenceContextProvider()
        {
            if (this._persistenceContextProvider is not null)
            {
                return this._persistenceContextProvider;
            }

            foreach (var server in this._gameServers.Values.OfType<GameServer>())
            {
                if (server.Context?.PersistenceContextProvider is { } provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private async Task<GameConfiguration?> GetGameConfigurationAsync(CancellationToken cancellationToken)
        {
            if (this._gameConfigurationSource is not null)
            {
                try
                {
                    return await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // continue to next resolution strategy
                }
            }

            foreach (var server in this._gameServers.Values.OfType<GameServer>())
            {
                if (server.Context?.Configuration is { } config)
                {
                    return config;
                }
            }

            return null;
        }

        private async Task<HashSet<string>> GetOnlineCharacterNamesAsync()
        {
            var onlineNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var server in this._gameServers.Values.OfType<GameServer>())
            {
                try
                {
                    var players = await server.Context.GetPlayersAsync().ConfigureAwait(false);
                    foreach (var p in players)
                    {
                        if (!string.IsNullOrEmpty(p.SelectedCharacter?.Name))
                        {
                            onlineNames.Add(p.SelectedCharacter.Name);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return onlineNames;
        }

        private async Task<(Guid? OwnerGuildId, HashSet<Guid> ChallengerGuildIds)> GetCastleSiegeGuildsAsync()
        {
            Guid? ownerGuildId = null;
            var challengerIds = new HashSet<Guid>();

            foreach (var server in this._gameServers.Values.OfType<GameServer>())
            {
                try
                {
                    var plugIn = server.Context.PlugInManager
                        .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
                        .OfType<CastleSiegePlugIn>()
                        .FirstOrDefault();
                    if (plugIn is not null)
                    {
                        var administration = new CastleSiegeAdministration(plugIn);
                        var snapshot = await administration.GetSnapshotAsync(server.Context).ConfigureAwait(false);
                        if (snapshot is not null)
                        {
                            ownerGuildId ??= snapshot.OwnerGuildId;
                            if (snapshot.Registrations is not null)
                            {
                                foreach (var reg in snapshot.Registrations)
                                {
                                    challengerIds.Add(reg.GuildId);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return (ownerGuildId, challengerIds);
        }

        private static (string ClassId, string ClassName, string ClassShort) GetClassInfo(int classNumber)
        {
            return classNumber switch
            {
                0 => ("dw", "Dark Wizard", "DW"),
                1 => ("dw", "Soul Master", "DW"),
                2 => ("dw", "Grand Master", "DW"),
                16 => ("dk", "Dark Knight", "DK"),
                17 => ("dk", "Blade Knight", "DK"),
                18 => ("dk", "Blade Master", "DK"),
                19 => ("dk", "Blade Master", "DK"),
                32 => ("elf", "Fairy Elf", "ELF"),
                33 => ("elf", "Muse Elf", "ELF"),
                34 => ("elf", "High Elf", "ELF"),
                35 => ("elf", "High Elf", "ELF"),
                48 => ("mg", "Magic Gladiator", "MG"),
                50 => ("mg", "Duel Master", "MG"),
                64 => ("dl", "Dark Lord", "DL"),
                66 => ("dl", "Lord Emperor", "DL"),
                80 => ("sum", "Summoner", "SUM"),
                81 => ("sum", "Bloody Summoner", "SUM"),
                82 => ("sum", "Dimension Master", "SUM"),
                83 => ("sum", "Dimension Master", "SUM"),
                96 => ("rf", "Rage Fighter", "RF"),
                98 => ("rf", "Fist Master", "RF"),
                _ => (classNumber >> 4) switch
                {
                    0 => ("dw", "Dark Wizard", "DW"),
                    1 => ("dk", "Dark Knight", "DK"),
                    2 => ("elf", "Fairy Elf", "ELF"),
                    3 => ("mg", "Magic Gladiator", "MG"),
                    4 => ("dl", "Dark Lord", "DL"),
                    5 => ("sum", "Summoner", "SUM"),
                    6 => ("rf", "Rage Fighter", "RF"),
                    _ => ("dk", "Blade Master", "DK"),
                },
            };
        }

        private async Task<List<CharacterRankEntry>> LoadAllCharactersAsync(IPersistenceContextProvider provider, CancellationToken cancellationToken)
        {
            using var context = provider.CreateNewContext();
            var dbConnection = TryGetDbConnection(context);
            if (dbConnection is not null)
            {
                try
                {
                    var sqlResult = await this.QueryCharactersFromDbAsync(dbConnection, cancellationToken).ConfigureAwait(false);
                    if (sqlResult.Count > 0)
                    {
                        return sqlResult;
                    }
                }
                catch
                {
                    // Fall back to entity context loader below
                }
            }

            return await this.QueryCharactersFallbackAsync(provider, cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<CharacterRankEntry>> QueryCharactersFromDbAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var list = new List<CharacterRankEntry>();
            var wasClosed = connection.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT c.""Id"", c.""Name"",
       COALESCE(MAX(CASE WHEN sa.""DefinitionId"" = '560931ad-0901-4342-b7f4-fd2e2fcc0563' THEN sa.""Value"" END), 1) as ""Level"",
       COALESCE(MAX(CASE WHEN sa.""DefinitionId"" = '70cd8c10-391a-4c51-9aa4-a854600e3a9f' THEN sa.""Value"" END), 0) as ""MasterLevel"",
       COALESCE(MAX(CASE WHEN sa.""DefinitionId"" = '89a891a7-f9f9-4ab5-af36-12056e53a5f7' THEN sa.""Value"" END), 0) as ""Resets"",
       g.""Name"" as ""GuildName"",
       cc.""Number"" as ""ClassNumber""
FROM data.""Character"" c
LEFT JOIN data.""StatAttribute"" sa ON sa.""CharacterId"" = c.""Id""
LEFT JOIN guild.""GuildMember"" gm ON gm.""Id"" = c.""Id""
LEFT JOIN guild.""Guild"" g ON g.""Id"" = gm.""GuildId""
LEFT JOIN config.""CharacterClass"" cc ON cc.""Id"" = c.""CharacterClassId""
GROUP BY c.""Id"", c.""Name"", g.""Name"", cc.""Number"";";

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var id = reader.GetGuid(0);
                    var name = await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false) ? string.Empty : reader.GetString(1);
                    var level = (int)Convert.ToDouble(reader.GetValue(2));
                    var masterLevel = (int)Convert.ToDouble(reader.GetValue(3));
                    var resets = (int)Convert.ToDouble(reader.GetValue(4));
                    var guild = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? string.Empty : reader.GetString(5);
                    var classNum = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? 0 : Convert.ToInt32(reader.GetValue(6));

                    var (classId, className, classShort) = GetClassInfo(classNum);
                    list.Add(new CharacterRankEntry(id, name, classId, className, classShort, level, masterLevel, resets, guild));
                }
            }
            finally
            {
                if (wasClosed)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }

            return list;
        }

        private async Task<List<CharacterRankEntry>> QueryCharactersFallbackAsync(IPersistenceContextProvider provider, CancellationToken cancellationToken)
        {
            var list = new List<CharacterRankEntry>();
            var gameConfig = await this.GetGameConfigurationAsync(cancellationToken).ConfigureAwait(false);
            if (gameConfig is null)
            {
                return list;
            }

            try
            {
                // Map Guilds for Guild names
                var characterGuildMap = new Dictionary<Guid, string>();
                try
                {
                    using var guildContext = provider.CreateNewGuildContext();
                    var guilds = await guildContext.GetGuildsOrderedByNameAsync(0, 500, cancellationToken).ConfigureAwait(false);
                    foreach (var g in guilds)
                    {
                        if (g.Members is not null && !string.IsNullOrEmpty(g.Name))
                        {
                            foreach (var member in g.Members)
                            {
                                characterGuildMap[member.Id] = g.Name;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                using var playerContext = provider.CreateNewPlayerContext(gameConfig);
                var accounts = await playerContext.GetAccountsOrderedByLoginNameAsync(0, 500, cancellationToken).ConfigureAwait(false);

                foreach (var accountHeader in accounts)
                {
                    try
                    {
                        var fullAccount = await playerContext.GetAccountByLoginNameAsync(accountHeader.LoginName, cancellationToken).ConfigureAwait(false);
                        if (fullAccount?.Characters is null)
                        {
                            continue;
                        }

                        foreach (var character in fullAccount.Characters)
                        {
                            var level = (int)(character.Attributes?.FirstOrDefault(a => a.Definition == Stats.Level || (a is MUnique.OpenMU.Persistence.IIdentifiable ident && ident.Id == LevelAttributeId))?.Value ?? 1);
                            var masterLevel = (int)(character.Attributes?.FirstOrDefault(a => a.Definition == Stats.MasterLevel || (a is MUnique.OpenMU.Persistence.IIdentifiable ident && ident.Id == MasterLevelAttributeId))?.Value ?? 0);
                            var resets = (int)(character.Attributes?.FirstOrDefault(a => a.Definition == Stats.Resets || (a is MUnique.OpenMU.Persistence.IIdentifiable ident && ident.Id == ResetsAttributeId))?.Value ?? 0);
                            var classNum = character.CharacterClass?.Number ?? 0;

                            var (classId, className, classShort) = GetClassInfo(classNum);
                            characterGuildMap.TryGetValue(character.Id, out var guildName);

                            list.Add(new CharacterRankEntry(
                                character.Id,
                                character.Name ?? string.Empty,
                                classId,
                                className,
                                classShort,
                                level,
                                masterLevel,
                                resets,
                                guildName ?? string.Empty));
                        }
                    }
                    catch
                    {
                        // continue with other accounts
                    }
                }
            }
            catch
            {
                // ignore
            }

            return list;
        }

        private async Task<List<GuildRankEntry>> LoadAllGuildsAsync(
            IPersistenceContextProvider provider,
            IReadOnlyDictionary<Guid, int> characterResets,
            CancellationToken cancellationToken)
        {
            var list = new List<GuildRankEntry>();
            try
            {
                using var guildContext = provider.CreateNewGuildContext();
                var guilds = await guildContext.GetGuildsOrderedByNameAsync(0, 1000, cancellationToken).ConfigureAwait(false);
                var (ownerGuildId, challengerIds) = await this.GetCastleSiegeGuildsAsync().ConfigureAwait(false);

                foreach (var g in guilds)
                {
                    var memberNames = await guildContext.GetMemberNamesAsync(g.Id).ConfigureAwait(false);
                    var masterMember = g.Members?.FirstOrDefault(m => m.Status == GuildPosition.GuildMaster);
                    var masterName = string.Empty;
                    if (masterMember is not null && memberNames.TryGetValue(masterMember.Id, out var foundMasterName))
                    {
                        masterName = foundMasterName;
                    }
                    else if (memberNames.Count > 0)
                    {
                        masterName = memberNames.Values.First();
                    }

                    var totalResets = 0;
                    if (g.Members is not null)
                    {
                        foreach (var member in g.Members)
                        {
                            if (characterResets.TryGetValue(member.Id, out var r))
                            {
                                totalResets += r;
                            }
                        }
                    }

                    var siegeStatus = "Alliance";
                    if (ownerGuildId.HasValue && g.Id == ownerGuildId.Value)
                    {
                        siegeStatus = "Castle Lord";
                    }
                    else if (challengerIds.Contains(g.Id))
                    {
                        siegeStatus = "Challenger";
                    }

                    var memberCountStr = $"{g.Members?.Count ?? 0}/40";

                    list.Add(new GuildRankEntry(
                        g.Id,
                        g.Name ?? string.Empty,
                        masterName,
                        g.Score,
                        totalResets,
                        memberCountStr,
                        siegeStatus));
                }
            }
            catch
            {
                // ignore
            }

            return list;
        }

        private static DbConnection? TryGetDbConnection(object context)
        {
            try
            {
                var contextProp = context.GetType().GetProperty("Context", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var dbContext = contextProp?.GetValue(context);
                if (dbContext is null)
                {
                    return null;
                }

                var dbFacadeProp = dbContext.GetType().GetProperty("Database", BindingFlags.Instance | BindingFlags.Public);
                var dbFacade = dbFacadeProp?.GetValue(dbContext);
                if (dbFacade is null)
                {
                    return null;
                }

                var relationalType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions"))
                    .FirstOrDefault(t => t != null);

                var getDbConnMethod = relationalType?.GetMethod("GetDbConnection", BindingFlags.Static | BindingFlags.Public);
                if (getDbConnMethod != null)
                {
                    return getDbConnMethod.Invoke(null, new[] { dbFacade }) as DbConnection;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private sealed record CharacterRankEntry(
            Guid Id,
            string Name,
            string ClassId,
            string ClassName,
            string ClassShort,
            int Level,
            int MasterLevel,
            int Resets,
            string GuildName);

        private sealed record GuildRankEntry(
            Guid Id,
            string Name,
            string Master,
            int Score,
            int TotalResets,
            string Members,
            string SiegeStatus);
    }
}
