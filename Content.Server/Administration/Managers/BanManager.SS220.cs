

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.SS220.Players;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedSpeciesBans = [];
    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedChatsBans = [];

    private void RestartAdditionalBans()
    {
        foreach (var bans in _cachedSpeciesBans.Values)
        {
            bans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }

        foreach (var bans in _cachedChatsBans.Values)
        {
            bans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }


    #region Species ban

    public HashSet<ProtoId<SpeciesPrototype>>? GetSpeciesBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return GetSpeciesBans(session);
    }

    public HashSet<ProtoId<SpeciesPrototype>>? GetSpeciesBans(ICommonSession session)
    {
        if (!_cachedSpeciesBans.TryGetValue(session, out var speciesBans))
            return null;

        if (speciesBans.Any(x => x.Roles is null))
        {
            _sawmill.Error($"Got empty specie ban for player {session.UserId}: {session.Name}!");
            speciesBans = [.. speciesBans.Where(x => x.Roles is not null)];
        }

        return [.. speciesBans.SelectMany(b => b.Roles!.Value).OfType<BanSpecieDef>().Select(x => x.Specie)];
    }

    public bool IsSpeciesBanned(ICommonSession pSession, ProtoId<SpeciesPrototype> specie)
    {
        return GetSpeciesBans(pSession)?.Contains(specie) is true;
    }

    public async void CreateSpeciesBan(CreateSpeciesBanInfo banInfo)
    {
        var adminName = banInfo.BanningAdmin == null
            ? Loc.GetString("system-user")
            : banInfo.BanningAdminName ?? (await _db.GetPlayerRecordByUserId(banInfo.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        ImmutableArray<IBanRoleDef> speciesDefs = [.. banInfo.SpeciesPrototypes.Where(x => _prototypeManager.HasIndex(x)).Select(x => new BanSpecieDef(x))];
        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Species, speciesDefs, adminName);

        await AddSpeciesBan(banDef);

        var length = expires == null ? Loc.GetString("cmd-species-ban-inf") : Loc.GetString("cmd-species-ban-until", ("expires", expires));

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));
        var speciesId = string.Join(", ", banInfo.SpeciesPrototypes);

        _chat.SendAdminAlert(Loc.GetString("cmd-species-ban-success", ("target", targetName), ("species", speciesId), ("reason", banInfo.Reason), ("length", length)));

        if (banInfo.PostBanInfo && banDef.Id is { } banId)
        {
            await _discordBanPostManager.PostUserBanInfo(banId);
        }

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
                SendSpeciesBans(session);
        }
    }

    public async Task<string> PardonSpeciesBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetBanAsync(banId);

        if (ban == null)
            return $"No ban found with id {banId}";

        if (ban.Type != BanType.Species)
            throw new InvalidOperationException("Ban was not a species ban!");

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

        await _db.AddUnbanAsync(new UnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        foreach (var user in ban.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var session)
                && _cachedSpeciesBans.TryGetValue(session, out var speciesBans))
            {
                speciesBans.RemoveAll(speciesBan => speciesBan.Id == ban.Id);
                SendSpeciesBans(session);
            }

        }

        return $"Pardoned species ban with id {banId}";
    }

    public void SendSpeciesBans(ICommonSession pSession)
    {
        var speciesBans = GetSpeciesBans(pSession);

        var bans = new MsgSpeciesBans
        {
            Bans = speciesBans is { } bannedSpecies ? [.. bannedSpecies] : []
        };

        _sawmill.Debug($"Sent species bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    private async Task AddSpeciesBan(BanDef banDef)
    {
        banDef = await _db.AddBanAsync(banDef);

        foreach (var user in banDef.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var player)
                && _cachedSpeciesBans.TryGetValue(player, out var cachedBans))
            {
                cachedBans.Add(banDef);
            }
        }
    }

    #endregion


    #region Chat ban

    public HashSet<BannableChats>? GetChatsBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return GetChatsBans(session);
    }

    public HashSet<BannableChats>? GetChatsBans(ICommonSession session)
    {
        if (!_cachedChatsBans.TryGetValue(session, out var chatsBans))
            return null;

        if (chatsBans.Any(x => x.Roles is null))
        {
            _sawmill.Error($"Got empty chat ban for player {session.UserId}: {session.Name}!");
            chatsBans = [.. chatsBans.Where(x => x.Roles is not null)];
        }

        return [.. chatsBans.SelectMany(b => b.Roles!.Value).OfType<BanChatDef>().Select(x => x.Chat)];
    }


    public bool IsChatBanned(ICommonSession pSession, BannableChats chat)
    {
        return GetChatsBans(pSession)?.Contains(chat) is true;
    }


    public void SendChatsBans(ICommonSession pSession)
    {
        var chatsBans = GetChatsBans(pSession);

        var bans = new MsgChatsBans
        {
            Bans = chatsBans is { } bannedChats ? [.. bannedChats] : []
        };

        _sawmill.Debug($"Sent chats bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    public async void CreateChatsBan(CreateChatsBanInfo banInfo)
    {
        var adminName = banInfo.BanningAdmin == null
            ? Loc.GetString("system-user")
            : banInfo.BanningAdminName ?? (await _db.GetPlayerRecordByUserId(banInfo.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        ImmutableArray<IBanRoleDef> chatDefs = [.. banInfo.Chats.Where(x => x is not BannableChats.Invalid).Select(x => new BanChatDef(x))];
        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Chat, chatDefs, adminName);

        await AddChatsBan(banDef);

        var length = expires == null ? Loc.GetString("cmd-chat-ban-inf") : Loc.GetString("cmd-chat-ban-until", ("expires", expires));

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));
        var bannedChats = string.Join(", ", banInfo.Chats);

        _chat.SendAdminAlert(Loc.GetString("cmd-chat-ban-success", ("target", targetName), ("chat", bannedChats), ("reason", banInfo.Reason), ("length", length)));

        if (banInfo.PostBanInfo && banDef.Id is { } banId)
        {
            await _discordBanPostManager.PostUserBanInfo(banId);
        }

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
                SendChatsBans(session);
        }
    }

    public async Task<string> PardonChatsBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetBanAsync(banId);

        if (ban == null)
            return $"No ban found with id {banId}";

        if (ban.Type != BanType.Chat)
            throw new InvalidOperationException("Ban was not a chat ban!");

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

        await _db.AddUnbanAsync(new UnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        foreach (var user in ban.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var session)
                && _cachedChatsBans.TryGetValue(session, out var chatsBans))
            {
                chatsBans.RemoveAll(chatBan => chatBan.Id == ban.Id);
                SendChatsBans(session);
            }
        }

        return $"Pardoned chats ban with id {banId}";
    }

    private async Task AddChatsBan(BanDef banDef)
    {
        banDef = await _db.AddBanAsync(banDef);

        foreach (var user in banDef.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var player)
                && _cachedChatsBans.TryGetValue(player, out var cachedBans))
            {
                cachedBans.Add(banDef);
            }
        }
    }

    #endregion
}
