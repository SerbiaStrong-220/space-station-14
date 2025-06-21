// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Pinpointer;
using Content.Shared.SS220.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Pinpointer.UI;

[UsedImplicitly]
public sealed partial class PinpointerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PinpointerMenu? _crewMenu;
    private PinpointerUplinkMenu? _itemMenu;

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
        {
            case PinpointerCrewUIState crewState:
                if (_crewMenu == null)
                    return;

                _crewMenu.CrewListCoords = crewState.Sensors;
                _crewMenu.PopulateList();
                break;

            case PinpointerItemUIState itemState:
                if (_itemMenu == null)
                    return;

                _itemMenu.ItemListSet = itemState.Items;
                _itemMenu.PopulateList();
                break;

            case PinpointerComponentUIState targetState:
                if (_crewMenu == null)
                    return;

                _crewMenu.CrewListCoords = targetState.Targets;
                _crewMenu.PopulateList();
                break;
        }
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<PinpointerComponent>(Owner, out var pinpointer))
            return;

        switch (pinpointer.Mode)//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
        {
            case PinpointerMode.Crew:
                {
                    _crewMenu = this.CreateWindow<PinpointerMenu>();
                    _crewMenu.OnTargetPicked = OnCrewTargetPicked;
                    _crewMenu.PopulateList();
                    break;
                }

            case PinpointerMode.Item:
                {
                    _itemMenu = this.CreateWindow<PinpointerUplinkMenu>();
                    _itemMenu.OnTargetPicked = OnTargetPicked;
                    _itemMenu.OnDnaPicked = OnDnaPicked;
                    _itemMenu.PopulateList();
                    break;
                }

            case PinpointerMode.Component:
                {
                    _crewMenu = this.CreateWindow<PinpointerMenu>();
                    _crewMenu.OnTargetPicked = OnTargetPicked;
                    _crewMenu.PopulateList();
                    break;
                }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _crewMenu?.Close();
        _itemMenu?.Close();
        _itemMenu?.DnaWindow?.Close();
    }

    private void OnCrewTargetPicked(NetEntity target)
    {
        SendMessage(new PinpointerCrewTargetPick(target));
    }
    private void OnTargetPicked(NetEntity target)
    {
        SendMessage(new PinpointerTargetPick(target));
    }

    private void OnDnaPicked(string? dna)
    {
        SendMessage(new PinpointerDnaPick(dna));
    }
}
