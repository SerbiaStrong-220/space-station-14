// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Database.SS220.PendingBans;
using Robust.Shared.Player;

namespace Content.Server.Administration.Managers;

public partial interface IBanManager
{
    Task ScheduleBanAsync(CreateBanInfo info, PendingBanData pending);
    Task ApplyPendingBanAsync(ActivatedPendingBan activated);
}

public sealed partial class BanManager
{
    public async Task ScheduleBanAsync(CreateBanInfo info, PendingBanData pending)
    {
        // Preserve the stricter validation of scheduled requests before shared preparation
        // filters unsupported entries for the existing immediate-ban commands.
        if (info is CreateSpeciesBanInfo species && species.SpeciesPrototypes.Any(p => !_prototypeManager.HasIndex(p)))
            throw new ArgumentException("Unknown species in pending ban.");
        if (info is CreateChatsBanInfo chats && chats.Chats.Any(c => c == BannableChats.Invalid || !Enum.IsDefined(c)))
            throw new ArgumentException("Unknown chat in pending ban.");

        var (ban, _, adminName) = await CreateBanDef(info);
        ban.Pending = pending;
        var saved = await _db.AddBanAsync(ban);
        _sawmill.Info($"Scheduled {ban.Type} ban #{saved.Id} by {adminName}; trigger: {pending.Trigger}");
    }

    /// <summary>
    /// Shared preparation for immediate and pending bans. Keep target/role validation and
    /// administrator lookup identical regardless of when the ban will take effect.
    /// </summary>
    private async Task<(BanDef Ban, DateTimeOffset? Expires, string AdminName)> CreateBanDef(CreateBanInfo info)
    {
        ImmutableArray<IBanRoleDef>? roles = null;
        BanType type;
        switch (info)
        {
            case CreateServerBanInfo:
                type = BanType.Server;
                break;
            case CreateRoleBanInfo role:
                type = BanType.Role;
                roles = [.. ToBanRoleDef(role.JobPrototypes), .. ToBanRoleDef(role.AntagPrototypes)];
                break;
            case CreateSpeciesBanInfo species:
                type = BanType.Species;
                roles = [.. species.SpeciesPrototypes.Where(p => _prototypeManager.HasIndex(p)).Select(p => new BanSpecieDef(p))];
                break;
            case CreateChatsBanInfo chats:
                type = BanType.Chat;
                roles = [.. chats.Chats.Where(c => c != BannableChats.Invalid).Select(c => new BanChatDef(c))];
                break;
            default:
                throw new ArgumentException("Unknown ban type.");
        }

        var adminName = info.BanningAdmin == null
            ? Loc.GetString("system-user")
            : info.BanningAdminName ?? (await _db.GetPlayerRecordByUserId(info.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var (ban, expires) = await CreateBanDef(info, type, roles, adminName);
        return (ban, expires, adminName);
    }

    /// <summary>
    /// Database activation has already committed. Apply the effects to connected players as well.
    /// </summary>
    public async Task ApplyPendingBanAsync(ActivatedPendingBan activated)
    {
        var ban = await _db.GetBanAsync(activated.Id);
        if (ban == null || ban.Pending != null || ban.Unban != null)
            return;

        if (ban.Type == BanType.Server && activated.Erase &&
            _systems.TryGetEntitySystem<AdminSystem>(out var adminSystem))
        {
            foreach (var user in ban.UserIds)
            {
                try
                {
                    // Erase must also work for an offline target.
                    adminSystem.Erase(user);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Could not erase player for pending ban #{ban.Id}: {e}");
                }
            }
        }

        foreach (var player in _playerManager.Sessions.ToArray())
        {
            if (!BanMatchesPlayer(player, ban))
                continue;

            switch (ban.Type)
            {
                case BanType.Server:
                    KickForBanDef(player, ban);
                    break;
                case BanType.Role:
                    AddPendingBanToCache(_cachedRoleBans, player, ban);
                    SendRoleBans(player);
                    break;
                case BanType.Species:
                    AddPendingBanToCache(_cachedSpeciesBans, player, ban);
                    SendSpeciesBans(player);
                    break;
                case BanType.Chat:
                    AddPendingBanToCache(_cachedChatsBans, player, ban);
                    SendChatsBans(player);
                    break;
            }
        }

        _sawmill.Info($"Activated pending {ban.Type} ban #{ban.Id}");
        _chat.SendAdminAlert(Loc.GetString("pending-ban-activated", ("id", activated.Id)));

        if (ban.Type == BanType.Server && _cfg.GetCVar(CCVars.ServerBanResetLastReadRules))
        {
            foreach (var user in ban.UserIds)
                await _db.SetLastReadRules(user, null);
        }

        // External webhooks must not hold server startup/shutdown hostage.
        if (activated.PostBanInfo)
            _ = PostPendingBanAsync(activated.Id);
    }

    private static void AddPendingBanToCache(
        Dictionary<ICommonSession, List<BanDef>> cache, ICommonSession player, BanDef ban)
    {
        if (!cache.TryGetValue(player, out var bans))
            cache[player] = bans = [];
        if (bans.All(b => b.Id != ban.Id))
            bans.Add(ban);
    }

    private async Task PostPendingBanAsync(int id)
    {
        try
        {
            await _discordBanPostManager.PostUserBanInfo(id);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to post pending ban #{id}: {e}");
        }
    }
}
