// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.SS220.ResourceMiner;

namespace Content.Client.SS220.ResourceMiner;

public sealed class ResourceMinerSettingsBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ResourceMinerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new ResourceMinerWindow();

        _window.OnClose += Close;
        _window.OnChangeSiloOption += OnChangeSilo;
        _window.OnRequestSilos += () => SendMessage(new RequestAvailableSilos());
        _window.OpenCentered();
    }

    private void OnChangeSilo(int chosenSiloNetId)
    {
        var chosenSiloNetEntity = new NetEntity(chosenSiloNetId);

        SendMessage(new SetResourceMinerSilo(chosenSiloNetEntity));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case AvailableSilosMiner silosState:
                _window?.SetAvailableSilos(silosState.Silos);
                break;
        }
    }
}
