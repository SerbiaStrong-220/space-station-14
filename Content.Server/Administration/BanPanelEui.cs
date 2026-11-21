using System.Net;
using System.Net.Sockets;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Content.Server.SS220.PendingBans;
using Content.Shared.Database.SS220.PendingBans;

namespace Content.Server.Administration;

public sealed class BanPanelEui : BaseEui
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;
    private bool _submitting; // SS220 pending bans

    private NetUserId? PlayerId { get; set; }
    private string PlayerName { get; set; } = string.Empty;
    private IPAddress? LastAddress { get; set; }
    private ImmutableTypedHwid? LastHwid { get; set; }
    private const int Ipv4_CIDR = CreateBanInfo.DefaultMaskIpv4;
    private const int Ipv6_CIDR = CreateBanInfo.DefaultMaskIpv6;

    public BanPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.bans_eui");
    }

    public override EuiStateBase GetNewState()
    {
        var hasBan = _admins.HasAdminFlag(Player, AdminFlags.Ban);
        return new BanPanelEuiState(PlayerName, hasBan);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case BanPanelEuiStateMsg.CreateBanRequest r:
                BanPlayer(r.Ban);
                break;
            case BanPanelEuiStateMsg.GetPlayerInfoRequest r:
                ChangePlayer(r.PlayerUsername);
                break;
        }
    }

    // SS220 pending bans begin
    // reject duplicate submits while resolving/saving the request.
    private async void BanPlayer(Ban ban)
    {
        if (_submitting)
            return;

        _submitting = true;
        try
        {
            await BanPlayerAsync(ban);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to submit ban: {e}");
            _chat.DispatchServerMessage(Player, Loc.GetString("pending-ban-failed"));
        }
        finally
        {
            _submitting = false;
        }
    }

    private async System.Threading.Tasks.Task BanPlayerAsync(Ban ban)
    // SS220 pending bans end
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to create a ban with no ban flag");

            return;
        }

        if (ban.Target == null && string.IsNullOrWhiteSpace(ban.IpAddress) && ban.Hwid == null)
        {
            _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-no-data"));

            return;
        }

        // SS220 pending bans begin
        // validate network input before doing any work.
        if (!Enum.IsDefined(ban.PendingTrigger) ||
            ban.PendingTrigger == PendingBanTrigger.Delay && (ban.DelayMinutes == 0 || ban.DelayMinutes > 525600) ||
            !Enum.IsDefined(ban.BanType) || string.IsNullOrWhiteSpace(ban.Reason))
        {
            _chat.DispatchServerMessage(Player, Loc.GetString("pending-ban-invalid"));
            return;
        }
        // SS220 pending bans end
        // SS220-more-reasonable-ban-type-begin
        // CreateBanInfo banInfo = isRoleBan ? new CreateRoleBanInfo(ban.Reason) : new CreateServerBanInfo(ban.Reason); [wizden]
        CreateBanInfo banInfo = ban.BanType switch
        {
            BanType.Chat => new CreateChatsBanInfo(ban.Reason),
            BanType.Species => new CreateSpeciesBanInfo(ban.Reason),
            BanType.Role => new CreateRoleBanInfo(ban.Reason),
            BanType.Server => new CreateServerBanInfo(ban.Reason),
            _ => throw new NotImplementedException($"Ban type {ban.BanType} has no implementations!"),
        };
        // SS220-more-reasonable-ban-type-end

        banInfo.WithBanningAdmin(Player.UserId);
        banInfo.WithBanningAdminName(Player.Name); // SS220-add-admin-name-in-ban
        banInfo.WithPostBanInfo(ban.PostBanInfo); // SSS220-add-post-ban-info
        banInfo.WithSeverity(ban.Severity);
        if (ban.BanDurationMinutes > 0)
            banInfo.WithMinutes(ban.BanDurationMinutes);

        (IPAddress, int)? addressRange = null;
        if (ban.IpAddress is not null)
        {
            if (!IPAddress.TryParse(ban.IpAddress, out var ipAddress) || !uint.TryParse(ban.IpAddressHid, out var hidInt) || hidInt > Ipv6_CIDR || hidInt > Ipv4_CIDR && ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-invalid-ip"));
                return;
            }

            if (hidInt == 0)
                hidInt = (uint) (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR);

            addressRange = (ipAddress, (int) hidInt);
        }

        var targetUid = ban.Target is not null ? PlayerId : null;
        addressRange = ban.UseLastIp && LastAddress is not null ? (LastAddress, LastAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR) : addressRange;
        var targetHWid = ban.UseLastHwid ? LastHwid : ban.Hwid;
        if (ban.Target != null && ban.Target != PlayerName || Guid.TryParse(ban.Target, out var parsed) && parsed != PlayerId)
        {
            var located = await _playerLocator.LookupIdByNameOrIdAsync(ban.Target);
            if (located == null)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("cmd-ban-player"));
                return;
            }
            targetUid = located.UserId;
            var targetAddress = located.LastAddress;
            if (ban.UseLastIp && targetAddress != null)
            {
                if (targetAddress.IsIPv4MappedToIPv6)
                    targetAddress = targetAddress.MapToIPv4();

                // Ban /64 for IPv6, /32 for IPv4.
                var hid = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR;
                addressRange = (targetAddress, hid);
            }
            targetHWid = ban.UseLastHwid ? located.LastHWId : ban.Hwid;
        }

        if (addressRange != null)
            banInfo.AddAddressRange(addressRange.Value);

        if (targetUid != null)
            banInfo.AddUser(targetUid.Value, ban.Target!);

        banInfo.AddHWId(targetHWid);


        switch (ban.BanType)
        {
            case BanType.Chat:
                var chatBanInfo = (CreateChatsBanInfo)banInfo;
                foreach (var chat in ban.BannedChats ?? [])
                {
                    chatBanInfo.AddChat(chat);
                }
                // SS220 pending bans begin
                if (ban.PendingTrigger == PendingBanTrigger.Immediate)
                    _banManager.CreateChatsBan(chatBanInfo);
                // SS220 pending bans end
                break;

            case BanType.Species:
                var speciesBanInfo = (CreateSpeciesBanInfo)banInfo;
                foreach (var specie in ban.BannedSpecies ?? [])
                {
                    speciesBanInfo.AddSpecie(specie);
                }
                // SS220 pending bans begin
                if (ban.PendingTrigger == PendingBanTrigger.Immediate)
                    _banManager.CreateSpeciesBan(speciesBanInfo);
                // SS220 pending bans end
                break;

            case BanType.Role:
                var roleBanInfo = (CreateRoleBanInfo)banInfo;
                foreach (var row in ban.BannedJobs ?? [])
                {
                    roleBanInfo.AddJob(row);
                }

                foreach (var row in ban.BannedAntags ?? [])
                {
                    roleBanInfo.AddAntag(row);
                }
                // SS220 pending bans begin
                if (ban.PendingTrigger == PendingBanTrigger.Immediate)
                    _banManager.CreateRoleBan(roleBanInfo);
                // SS220 pending bans end
                break;

            case BanType.Server:
                if (ban.PendingTrigger == PendingBanTrigger.Immediate && ban.Erase && targetUid is not null) // SS220 pending bans
                {
                    try
                    {
                        if (_entities.TrySystem(out AdminSystem? adminSystem))
                            adminSystem.Erase(targetUid.Value);
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Error while erasing banned player:\n{e}");
                    }
                }

                if (ban.PendingTrigger == PendingBanTrigger.Immediate) // SS220 pending bans
                    _banManager.CreateServerBan((CreateServerBanInfo)banInfo);
                break;

            default:
                throw new NotImplementedException($"Ban type {ban.BanType} has no implementations!");
        }

        // SS220 pending bans begin
        // only acknowledge after the schedule is durably committed.
        if (ban.PendingTrigger != PendingBanTrigger.Immediate)
        {
            // Permissions can change while the target is being looked up.
            if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
                return;

            await _entities.System<PendingBanSystem>().ScheduleAsync(
                banInfo, ban.PendingTrigger, ban.DelayMinutes, ban.Erase, ban.PostBanInfo);
            _chat.DispatchServerMessage(Player, Loc.GetString("pending-ban-scheduled"));
        }
        // SS220 pending bans end

        Close();
    }

    public async void ChangePlayer(string playerNameOrId)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(playerNameOrId);
        ChangePlayer(located?.UserId, located?.Username ?? string.Empty, located?.LastAddress, located?.LastHWId);
    }

    public void ChangePlayer(NetUserId? playerId, string playerName, IPAddress? lastAddress, ImmutableTypedHwid? lastHwid)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        LastAddress = lastAddress;
        LastHwid = lastHwid;
        StateDirty();
    }

    public override async void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
        {
            return;
        }

        StateDirty();
    }
}
