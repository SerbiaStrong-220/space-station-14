// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.FungusMachine;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.SS220.CultYogg.FungusMachine;

public sealed class FungusMachineSystem : SharedFungusMachineSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
        });
    }

    protected override void OnComponentInit(Entity<FungusMachineComponent> ent, ref ComponentInit args)
    {
        base.OnComponentInit(ent, ref args);

        ent.Comp.Container = _containerSystem.EnsureContainer<Container>(ent.Owner, FungusMachineComponent.ContainerId);
    }

    private void OnBoundUIOpened(Entity<FungusMachineComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateFungusMachineInterfaceState(ent);
    }

    private void UpdateFungusMachineInterfaceState(Entity<FungusMachineComponent> ent)
    {
        var state = new FungusMachineInterfaceState(GetInventory(ent));

        _userInterfaceSystem.SetUiState(ent.Owner, FungusMachineUiKey.Key, state);
    }

    private FungusMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, FungusMachineComponent? component = null)
    {
        return !Resolve(uid, ref component) ? null : component.Inventory.GetValueOrDefault(entryId);
    }
}
