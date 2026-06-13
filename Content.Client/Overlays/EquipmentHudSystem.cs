using Content.Shared.SS220.Mech.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Player;
using Robust.Shared.Containers;//SS220 add mech overlay
using Robust.Shared.Player;

namespace Content.Client.Overlays;

/// <summary>
/// This is a base system to make it easier to enable or disabling UI elements based on whether or not the player has
/// some component, either on their controlled entity on some worn piece of equipment.
/// </summary>
public abstract class EquipmentHudSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [ViewVariables]
    public bool IsActive { get; private set; }
    protected virtual SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<T, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<T, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<T, GotUnequippedEvent>(OnCompUnequip);

        //SS220 add mech overlay begin
        SubscribeLocalEvent<T, EntGotInsertedIntoContainerMessage>(OnCompEquip);
        SubscribeLocalEvent<T, EntGotRemovedFromContainerMessage>(OnCompUnequip);
        //SS220 add mech overlay end

        SubscribeLocalEvent<T, RefreshEquipmentHudEvent<T>>(OnRefreshComponentHud);
        SubscribeLocalEvent<T, InventoryRelayedEvent<RefreshEquipmentHudEvent<T>>>(OnRefreshEquipmentHud);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void Update(RefreshEquipmentHudEvent<T> ev)
    {
        IsActive = true;
        UpdateInternal(ev);
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        DeactivateInternal();
    }

    protected virtual void UpdateInternal(RefreshEquipmentHudEvent<T> args) { }

    protected virtual void DeactivateInternal() { }

    private void OnStartup(Entity<T> ent, ref ComponentStartup args)
    {
        RefreshOverlay();
    }

    private void OnRemove(Entity<T> ent, ref ComponentRemove args)
    {
        RefreshOverlay();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_player.LocalSession?.AttachedEntity is null)
            Deactivate();
    }

    private void OnCompEquip(Entity<T> ent, ref GotEquippedEvent args)
    {
        RefreshOverlay();
    }

    private void OnCompUnequip(Entity<T> ent, ref GotUnequippedEvent args)
    {
        RefreshOverlay();
    }

    //SS220 add mech overlay begin
    private void OnCompEquip(Entity<T> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<AltMechComponent>(args.Container.Owner, out var _))
            RefreshOverlay();
    }

    private void OnCompUnequip(Entity<T> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<AltMechComponent>(args.Container.Owner, out var _))
            RefreshOverlay();
    }
    //SS220 add mech overlay end

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Deactivate();
    }

    protected virtual void OnRefreshEquipmentHud(Entity<T> ent, ref InventoryRelayedEvent<RefreshEquipmentHudEvent<T>> args)
    {
        OnRefreshComponentHud(ent, ref args.Args);
    }

    protected virtual void OnRefreshComponentHud(Entity<T> ent, ref RefreshEquipmentHudEvent<T> args)
    {
        args.Active = true;
        args.Components.Add(ent.Comp);
    }

    protected void RefreshOverlay()
    {
        if (_player.LocalSession?.AttachedEntity is not { } entity)
            return;

        var ev = new RefreshEquipmentHudEvent<T>(TargetSlots);
        RaiseLocalEvent(entity, ref ev);

        //SS220 add mech overlays begin
        if (TryComp<AltMechComponent>(entity, out var mechComp))//Honestly i'm not perfectly fine with this solution but it is way better than making hundreds of strings of code to rewrite the entire equipment overlay system and every overlay but for mech visors only
        {
            if (mechComp.ContainerDict["head"].ContainedEntity != null && TryComp<T>(mechComp.ContainerDict["head"].ContainedEntity, out var headComp))
            {
                ev.Active = true;
                ev.Components.Add(headComp);
            }

            if (mechComp.PilotSlot.ContainedEntity != null)
            {
                var evPilot = new RefreshEquipmentHudEvent<T>(TargetSlots);
                RaiseLocalEvent((EntityUid)mechComp.PilotSlot.ContainedEntity, ref evPilot);

                ev.Active = ev.Active || evPilot.Active;

                foreach (var comp in evPilot.Components)
                {
                    if (!ev.Components.Contains(comp))
                        ev.Components.Add(comp);
                }
            }
        }
        //SS220 add mech overlays end

        if (ev.Active)
            Update(ev);
        else
            Deactivate();
    }
}
