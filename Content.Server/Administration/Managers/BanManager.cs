using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.SS220.Database;
using Content.Server.SS220.Discord;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Content.Shared.SS220.Players;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;
    [Dependency] private readonly DiscordBanPostManager _discordBanPostManager = default!; // SS220-add-discord-post-ban-notify

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string PrefixAntag = "Antag:";
    public const string PrefixJob = "Job:";
    public const string PrefixSpecie = "Specie:"; // SS220-species-ban

    private readonly Dictionary<ICommonSession, List<ServerRoleBanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    private readonly Dictionary<ICommonSession, List<ServerSpeciesBanDef>> _cachedSpeciesBans = []; // SS220 Species bans

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();
        _netManager.RegisterNetMessage<MsgSpeciesBans>(); // SS220 Species bans

        _db.SubscribeToJsonNotification<BanNotificationData>(
            _taskManager,
            _sawmill,
            BanNotificationChannel,
            ProcessBanNotification,
            OnDatabaseNotificationEarlyFilter);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetServerRoleBansAsync(netChannel.RemoteEndPoint.Address, player.UserId, hwId, modernHwids, false);
        var speciesBans = await _db.GetServerSpeciesBansAsync(netChannel.RemoteEndPoint.Address, player.UserId, hwId, modernHwids, false); // SS220 Species bans

        var userRoleBans = new List<ServerRoleBanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;
        _cachedSpeciesBans[player] = [.. speciesBans]; // SS220 Species bans

        SendRoleBans(player);
        SendSpeciesBans(player); // SS220 Species bans
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }

        SpeciesBansRestart(); // SS220 Species bans
    }

    // SS220 Species bans begin
    private void SpeciesBansRestart()
    {
        foreach (var (player, bans) in _cachedSpeciesBans.ToDictionary())
        {
            // Clear out players that have disconnected.
            if (player.Status is SessionStatus.Disconnected)
            {
                _cachedSpeciesBans.Remove(player);
                continue;
            }

            // Check for expired bans
            bans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }
    // SS220 Species bans end

    #region Server Bans
    public async void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, uint? minutes, NoteSeverity severity, string? banningAdminName, int statedRound, string reason, bool postBanInfo)
    {
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            target,
            addressRange,
            hwid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            banningAdminName,
            statedRound,
            null);

        // await _db.AddServerBanAsync(banDef);  // SS220 user ban info post
        var banId = await _db.AddServerBanAsync(banDef); // SS220 user ban info post
        if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules) && target != null)
            await _db.SetLastReadRules(target.Value, null); // Reset their last read rules. They probably need a refresher!
        var adminName = banningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = target is null ? "null" : $"{targetUsername} ({target})";
        var addressRangeString = addressRange != null
            ? $"{addressRange.Value.Item1}/{addressRange.Value.Item2}"
            : "null";
        var hwidString = hwid?.ToString() ?? "null";
        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        // SS220 user ban info post start
        if (postBanInfo)
        {
            await _discordBanPostManager.PostUserBanInfo(banId);
        }
        // SS220 user ban info post end

        KickMatchingConnectedPlayers(banDef, "newly placed ban");
    }

    private void KickMatchingConnectedPlayers(ServerBanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, ServerBanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, ServerBanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager, player.Name); // SS220-ad-login-into-ban-screen
        player.Channel.Disconnect(message);
    }

    #endregion

    #region Role Bans

    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan<T>(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        ProtoId<T> role,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        DateTimeOffset timeOfBan,
        bool postBanInfo
    ) where T : class, IPrototype
    {
        string encodedRole;

        // TODO: Note that it's possible to clash IDs here between a job and an antag. The refactor that introduced
        // this check has consciously avoided refactoring Job and Antag prototype.
        // Refactor Job- and Antag- Prototype to introduce a common RolePrototype, which will fix this possible clash.

        //TODO remove this check as part of the above refactor
        if (_prototypeManager.HasIndex<JobPrototype>(role) && _prototypeManager.HasIndex<AntagPrototype>(role))
        {
            _sawmill.Error($"Creating role ban for {role}: cannot create role ban, role is both JobPrototype and AntagPrototype.");

            return;
        }

        // Don't trust the input: make sure the job or antag actually exists.
        if (_prototypeManager.HasIndex<JobPrototype>(role))
            encodedRole = PrefixJob + role;
        else if (_prototypeManager.HasIndex<AntagPrototype>(role))
            encodedRole = PrefixAntag + role;
        else if (_prototypeManager.HasIndex<SpeciesPrototype>(role)) // SS220-add-species-ban
            encodedRole = PrefixSpecie + role; // SS220-add-species-ban
        else
        {
            _sawmill.Error($"Creating role ban for {role}: cannot create role ban, role is not a JobPrototype or an AntagPrototype.");

            return;
        }

        DateTimeOffset? expires = null;

        if (minutes > 0)
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            encodedRole);

        if (!await AddRoleBan(banDef))
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));

            return;
        }

        // SS220 user ban info post start
        if (banDef.Id.HasValue && postBanInfo)
        {
            await _discordBanPostManager.PostUserJobBanInfo(banDef.Id.Value, targetUsername);
        }
        // SS220 user ban info post end

        var length = expires == null ? Loc.GetString("cmd-roleban-inf") : Loc.GetString("cmd-roleban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-roleban-success", ("target", targetUsername ?? "null"), ("role", role), ("reason", reason), ("length", length)));

        if (target is not null && _playerManager.TryGetSessionById(target.Value, out var session))
            SendRoleBans(session);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null
            && _playerManager.TryGetSessionById(banDef.UserId, out var player)
            && _cachedRoleBans.TryGetValue(player, out var cachedBans))
        {
            cachedBans.Add(banDef);
        }

        return true;
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        if (ban.UserId is { } player
            && _playerManager.TryGetSessionById(player, out var session)
            && _cachedRoleBans.TryGetValue(session, out var roleBans))
        {
            roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
            SendRoleBans(session);
        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId)
    {
        return GetRoleBans<JobPrototype>(playerUserId, PrefixJob);
    }

    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId)
    {
        return GetRoleBans<AntagPrototype>(playerUserId, PrefixAntag);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(NetUserId playerUserId, string prefix) where T : class, IPrototype
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return GetRoleBans<T>(session, prefix);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(ICommonSession playerSession, string prefix) where T : class, IPrototype
    {
        if (!_cachedRoleBans.TryGetValue(playerSession, out var roleBans))
            return null;

        return roleBans
            .Where(ban => ban.Role.StartsWith(prefix, StringComparison.Ordinal))
            .Select(ban => new ProtoId<T>(ban.Role[prefix.Length..]))
            .ToHashSet();
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
            ? roleBans.Select(banDef => banDef.Role).ToHashSet()
            : null;
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<JobPrototype>> jobs)
    {
        return IsRoleBanned(player, jobs, PrefixJob);
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<AntagPrototype>> antags)
    {
        return IsRoleBanned(player, antags, PrefixAntag);
    }

    private bool IsRoleBanned<T>(ICommonSession player, List<ProtoId<T>> roles, string prefix) where T : class, IPrototype
    {
        var bans = GetRoleBans(player.UserId);

        if (bans is null || bans.Count == 0)
            return false;

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var role in roles)
        {
            if (bans.Contains(prefix + role))
                return true;
        }

        return false;
    }

    // SS220 Species bans begin
    #region Species ban
    private async Task<ServerSpeciesBanDef> AddSpeciesBan(ServerSpeciesBanDef banDef)
    {
        banDef = await _db.AddServerSpeciesBanAsync(banDef);

        if (banDef.UserId != null
            && _playerManager.TryGetSessionById(banDef.UserId, out var player)
            && _cachedSpeciesBans.TryGetValue(player, out var cachedBans))
        {
            cachedBans.Add(banDef);
        }

        return banDef;
    }

    public HashSet<string>? GetSpeciesBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        if (!_cachedSpeciesBans.TryGetValue(session, out var speciesBans))
            return null;

        return [.. speciesBans.Select(banDef => banDef.SpeciesId)];
    }

    public bool IsSpeciesBanned(NetUserId playerUserId, SpeciesPrototype speciesPrototype)
    {
        return IsSpeciesBanned(playerUserId, speciesPrototype.ID);
    }

    public bool IsSpeciesBanned(NetUserId playerUserId, string speciesId)
    {
        return GetSpeciesBans(playerUserId)?.Contains(speciesId) is true;
    }

    public async void CreateSpeciesBan(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        string speciesId,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        DateTimeOffset timeOfBan,
        bool postBanInfo)
    {
        if (!_prototypeManager.HasIndex<SpeciesPrototype>(speciesId))
            throw new ArgumentException($"Invalid speicies id '{speciesId}'", nameof(speciesId));

        DateTimeOffset? expires = null;
        if (minutes > 0)
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null
            ? TimeSpan.Zero
            : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerSpeciesBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            speciesId);

        banDef = await AddSpeciesBan(banDef);

        if (banDef is null)
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-species-ban-existing", ("target", targetUsername ?? "null"), ("species", speciesId)));
            return;
        }

        var length = expires == null ? Loc.GetString("cmd-species-ban-inf") : Loc.GetString("cmd-species-ban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-species-ban-success", ("target", targetUsername ?? "null"), ("species", speciesId), ("reason", reason), ("length", length)));

        if (target != null && _playerManager.TryGetSessionById(target.Value, out var session))
            SendSpeciesBans(session);
    }

    public async Task<string> PardonSpeciesBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerSpeciesBanAsync(banId);

        if (ban == null)
            return $"No ban found with id {banId}";

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerSpeciesUnbanAsync(new ServerSpeciesUnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        if (ban.UserId is { } player
            && _playerManager.TryGetSessionById(player, out var session)
            && _cachedSpeciesBans.TryGetValue(session, out var speciesBans))
        {
            speciesBans.RemoveAll(speciesBan => speciesBan.Id == ban.Id);
            SendSpeciesBans(session);
        }

        return $"Pardoned ban with id {banId}";
    }

    public void SendSpeciesBans(ICommonSession pSession)
    {
        var speciesBans = _cachedSpeciesBans.GetValueOrDefault(pSession) ?? new List<ServerSpeciesBanDef>();
        var bans = new MsgSpeciesBans
        {
            Bans = [.. speciesBans.Select(b => b.SpeciesId)]
        };

        _sawmill.Debug($"Sent species bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }
    #endregion
    // SS220 Species bans end

    public void SendRoleBans(ICommonSession pSession)
    {
        var jobBans = GetRoleBans<JobPrototype>(pSession, PrefixJob);
        var jobBansList = new List<string>(jobBans?.Count ?? 0);

        if (jobBans is not null)
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var encodedId in jobBans)
            {
                jobBansList.Add(encodedId.ToString().Replace(PrefixJob, ""));
            }
        }

        var antagBans = GetRoleBans<AntagPrototype>(pSession, PrefixAntag);
        var antagBansList = new List<string>(antagBans?.Count ?? 0);

        if (antagBans is not null)
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var encodedId in antagBans)
            {
                antagBansList.Add(encodedId.ToString().Replace(PrefixAntag, ""));
            }
        }

        var bans = new MsgRoleBans()
        {
            JobBans = jobBansList,
            AntagBans = antagBansList,
        };

        _sawmill.Debug($"Sent role bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    #endregion

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
