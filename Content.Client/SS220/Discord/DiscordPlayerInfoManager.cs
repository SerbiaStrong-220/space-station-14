// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Discord;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client.SS220.Discord;

public sealed class DiscordPlayerInfoManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private DiscordSponsorInfo? _info;

    public event Action? SponsorStatusChanged;

    public string AuthUrl { get; private set; } = string.Empty;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>(UpdateSponsorStatus);
        _netMgr.RegisterNetMessage<MsgDiscordLinkRequired>(OnDiscordLinkRequired);
        _netMgr.RegisterNetMessage<MsgRecheckDiscordLink>();

        _netMgr.RegisterNetMessage<MsgByPassDiscordCheck>();
    }

    private void UpdateSponsorStatus(MsgUpdatePlayerDiscordStatus message)
    {
        _info = message.Info;

        SponsorStatusChanged?.Invoke();
    }

    public SponsorTier[] GetSponsorTier()
    {
        return _info?.Tiers ?? Array.Empty<SponsorTier>();
    }

    private void OnDiscordLinkRequired(MsgDiscordLinkRequired msg)
    {
        if (_stateManager.CurrentState is not DiscordLinkRequiredState)
        {
            AuthUrl = msg.AuthUrl;
            _stateManager.RequestStateChange<DiscordLinkRequiredState>();
        }
    }

    public void ByPassCheck()
    {
        _netMgr.ClientSendMessage(new MsgByPassDiscordCheck());
    }
}
