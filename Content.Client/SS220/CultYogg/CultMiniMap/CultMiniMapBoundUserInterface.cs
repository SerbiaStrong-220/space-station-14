// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface;
using Content.Shared.SS220.CultYogg.CultMiniMap;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

public sealed class CultMiniMapBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CultMiniMapWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CultMiniMapWindow>();
        _menu.PingRequested += coordinates =>
            SendMessage(new CultMiniMapPingMessage(EntMan.GetNetCoordinates(coordinates)));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CultMiniMapState mapState)
            _menu?.UpdateState(mapState, EntMan.GetNetEntity(Owner));
    }
}
