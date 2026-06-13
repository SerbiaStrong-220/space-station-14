// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Server.Atmos.Components;
using Content.Shared.SS220.AltMech;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;

namespace Content.Server.SS220.Mech.Systems;

/// <inheritdoc/>
public sealed partial class AltMechSystem
{
    private void OnRemoveEquipmentMessage(Entity<AltMechComponent> ent, ref AltMechEquipmentRemoveMessage args)
    {
        var equip = GetEntity(args.Equipment);

        if (!Exists(equip) || Deleted(equip))
            return;

        if (!TryComp<AltMechEquipmentComponent>(equip, out var equipmentComp))
            return;

        RemoveEquipment(ent.Owner, equip);
    }

    private void OnRemovePartMessage(Entity<AltMechComponent> ent, ref MechPartRemoveMessage args)
    {
        var equip = (ent.Comp.ContainerDict[args.Part].ContainedEntity);

        if (!Exists(equip) || Deleted(equip))
            return;

        RemovePart(ent.Owner, (EntityUid)equip);
    }

    private void OnMaintenanceToggledMessage(Entity<AltMechComponent> ent, ref MechMaintenanceToggleMessage args)
    {
        ent.Comp.MaintenanceMode = args.Toggled;
        Dirty(ent.Owner, ent.Comp);
    }

    public void OnTankDetachMessage(Entity<AltMechComponent> ent, ref MechDetachTankMessage args)
    {
        if (ent.Comp.TankSlot.ContainedEntity != null)
            _container.Remove(ent.Comp.TankSlot.ContainedEntity.Value, ent.Comp.TankSlot);
    }

    public void OnMechSealMessage(Entity<AltMechComponent> ent, ref MechSealMessage args)
    {
        ent.Comp.Airtight = args.Toggled;

        _audio.PlayPvs(ent.Comp.SealSound, ent.Owner);

        Dirty(ent);

        if (ent.Comp.PilotSlot == null || ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        _alerts.ShowAlert(ent.Owner, "Internals", GetSeverity(ent));

        if (TryComp<BarotraumaComponent>(ent.Comp.PilotSlot.ContainedEntity, out var barotraumaComp))
            barotraumaComp.HasImmunity = ent.Comp.Airtight && ent.Comp.Sealable;
    }

    public void OnMechBoltMessage(Entity<AltMechComponent> ent, ref MechBoltMessage args)
    {
        if (ent.Comp.BoltsSawed)
        {
            _popup.PopupEntity(Loc.GetString("mech-bolts-error"), ent.Owner);
            return;
        }

        ent.Comp.Bolted = args.Toggled;

        if (ent.Comp.Bolted)
        {
            _audio.PlayPvs(ent.Comp.BoltSound, ent.Owner);
            return;
        }
        _audio.PlayPvs(ent.Comp.UnboltSound, ent.Owner);

        Dirty(ent);
    }

    private void OnMechExit(Entity<AltMechComponent> ent, ref MechExitEvent args)
    {
        ExitMech(ent);
    }

    private void OnOpenUi(Entity<AltMechComponent> ent, ref MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(ent.Owner, ent.Comp);
    }
}
