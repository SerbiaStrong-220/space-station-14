// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Materials;
using Content.Shared.SS220.ResourceMiner;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ResourceMiner;

public sealed class ResourceMinerSettingsBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ResourceMinerWindow? _window;

    private NetEntity? _chosenSilo;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ResourceMinerWindow>();

        _window.OnChangeSiloOption += OnChangeSilo;
        _window.OnRequestSilos += () => SendMessage(new RequestAvailableSilos());

        if (!EntMan.TryGetComponent<ResourceMinerComponent>(Owner, out var resourceMinerComponent))
            return;

        _window.SetGenerationAmount(resourceMinerComponent.GenerationAmount);
        _window.SetTimeBetweenUpdate(resourceMinerComponent.TimeBetweenUpdate);
        _chosenSilo = EntMan.GetNetEntity(resourceMinerComponent.Silo);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
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
                _window?.SetAvailableSilos(silosState.Silos, _chosenSilo);
                break;
        }
    }
}
