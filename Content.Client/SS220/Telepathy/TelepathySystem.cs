// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.SS220.Telepathy;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Telepathy;

/// <summary>
/// Updates chat channels when the local player's telepathy component changes.
/// </summary>
public sealed class TelepathySystem : SharedTelepathySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private bool _channelPermissionsDirty;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelepathyChangedEvent>(OnTelepathyChanged);
    }

    private void OnTelepathyChanged(ref TelepathyChangedEvent ev)
    {
        if (ev.Entity != _player.LocalEntity)
            return;

        _channelPermissionsDirty = true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_channelPermissionsDirty)
            return;

        // Wait until component removal and network state application have finished.
        _channelPermissionsDirty = false;
        _ui.GetUIController<ChatUIController>().RefreshChannelPermissions();
    }
}
