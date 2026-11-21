using Content.Server.Mind;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Items;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;
using Robust.Shared.Containers;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Marauder;

public sealed class RatvarMarauderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarMarauderShellComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<RatvarMarauderShellComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RatvarMarauderShellComponent, ContainerIsInsertingAttemptEvent>(OnSoulVesselInsertAttempt);
        SubscribeLocalEvent<RatvarMarauderShellComponent, ComponentInit>(OnMarauderInit);
        SubscribeLocalEvent<RatvarMarauderShellComponent, ComponentShutdown>(OnMarauderShutdown);
    }

    private void OnMindRemoved(EntityUid uid, RatvarMarauderShellComponent component, MindRemovedMessage args)
    {
        _appearanceSystem.SetData(uid, RatvarSoulVesselVisualState.State, false);
    }

    private void OnMindAdded(EntityUid uid, RatvarMarauderShellComponent component, MindAddedMessage args)
    {
        _appearanceSystem.SetData(uid, RatvarSoulVesselVisualState.State, true);
    }

    private void OnSoulVesselInsertAttempt(EntityUid uid, RatvarMarauderShellComponent component,
        ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != component.SoulVesselSlotId)
            return;

        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out _))
        {
            args.Cancel();
            return;
        }

        _mindSystem.TransferTo(mindId, uid);
        _slotsSystem.SetLock(uid, component.SoulVesselSlotId, true);

        QueueDel(args.EntityUid);
    }

    private void OnMarauderInit(EntityUid uid, RatvarMarauderShellComponent component, ComponentInit args)
    {
        _slotsSystem.AddItemSlot(uid, component.SoulVesselSlotId, component.SoulVesselSlot);
    }

    private void OnMarauderShutdown(EntityUid uid, RatvarMarauderShellComponent component, ComponentShutdown args)
    {
        _slotsSystem.RemoveItemSlot(uid, component.SoulVesselSlot);
    }
}
