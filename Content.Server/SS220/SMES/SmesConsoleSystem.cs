// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.SMES;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.SMES;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.SMES;

public sealed class SmesConsoleSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmesComponent, BoundUIOpenedEvent>(UpdateUserInterface);
    }

    private void UpdateUserInterface(EntityUid uid, SmesComponent? component, BoundUIOpenedEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<MetaDataComponent>(uid, out var metaDataComponent))
            return;

        if (!TryComp<DeviceNetworkComponent>(uid, out var deviceNetworkComp))
            return;

        if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
            return;

        var state = new SmesState(
            metaDataComponent.EntityName,
            deviceNetworkComp.Address,
            (int) batteryComponent.CurrentCharge / 1000,
            (int) batteryComponent.MaxCharge / 1000,
            _batterySystem.GetChargePercentRounded(batteryComponent)
            );
        _userInterface.SetUiState(uid, SmesUiKey.Key, state);
    }
}
