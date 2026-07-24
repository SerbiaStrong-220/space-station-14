// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Movement.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Parts.Components;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the power of the mech
/// </summary>
public sealed partial class AltMechSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AltMechComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<InputMoverComponent>(uid, out var moverComp))
                continue;

            if (comp.PilotSlot.ContainedEntity == null)
                continue;

            if (curTime < comp.NextPowerDrain)
                continue;

            if (comp.ContainerDict["chassis"].ContainedEntity is not { Valid: true } chassisValid)
                continue;

            if (!TryComp<MechChassisComponent>(chassisValid, out var chassisComp))
                continue;

            comp.NextPowerDrain = _timing.CurTime + new TimeSpan(0, 0, 1);

            var drainedEnergy = (comp.OverallMass * chassisComp.Efficiency);

            if (comp.OverallMass >= comp.MaximalMass * 2)
                drainedEnergy = comp.MaximalMass * 2 * chassisComp.Efficiency;

            TryChangeMechCharge((uid, comp), -(drainedEnergy).Float());
        }
    }

    public bool TryChangeMechCharge(Entity<AltMechComponent> ent, float amount)
    {
        if (ent.Comp.ContainerDict["power"].ContainedEntity is not { Valid: true } batteryValid)
            return false;

        if (!TryComp<BatteryComponent>(ent.Comp.ContainerDict["power"].ContainedEntity, out var batteryComp))
            return false;

        var actualChange = _battery.ChangeCharge(batteryValid, amount);
        ent.Comp.Energy = batteryComp.LastCharge;

        UpdateUserInterface(ent);
        Dirty(ent);

        if (actualChange == 0 && amount != 0)
            return false;

        return true;
    }

    private void OnChargeChanged(Entity<MechPartComponent> ent, ref ChargeChangedEvent args)
    {
        if (ent.Comp.PartOwner is { Valid: true} mechValidated)
            UpdateMechOnlineStatus(mechValidated, ent.Owner);
    }

    public void UpdateMechOnlineStatus(EntityUid mech, EntityUid battery)
    {
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Energy <= 0 && mechComp.Online)
        {
            mechComp.Online = false;

            TransferMindIntoPilot((mech, mechComp));

            if (mechComp.ContainerDict["chassis"].ContainedEntity is not { Valid: true })
                _actionBlocker.UpdateCanMove(mech);
        }
        if (mechComp.Energy > 0 && !mechComp.Online)
        {
            mechComp.Online = true;

            if (mechComp.PilotSlot.ContainedEntity is not { Valid: true })
                TransferMindIntoMech((mech, mechComp));

            if (mechComp.ContainerDict["chassis"].ContainedEntity is not { Valid: true })
                _actionBlocker.UpdateCanMove(mech);
        }
    }

}
