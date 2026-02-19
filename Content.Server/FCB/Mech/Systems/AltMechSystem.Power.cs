// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Movement.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Parts.Components;
using Robust.Shared.Timing;

namespace Content.Server.FCB.Mech.Systems;

/// <summary>
/// Handles the power of the mech
/// </summary>
public sealed partial class AltMechSystem
{
    [Dependency] protected readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AltMechComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<InputMoverComponent>(uid, out var moverComp))
                return;

            if (comp.PilotSlot.ContainedEntity == null)
                return;

            if (curTime < comp.NextPowerDrain)
                continue;

            if (comp.ContainerDict["chassis"].ContainedEntity == null)
                return;

            var batteryEnt = comp.ContainerDict["power"].ContainedEntity;

            if (batteryEnt == null)
                return;

            if (!TryComp<BatteryComponent>(comp.ContainerDict["power"].ContainedEntity, out var batteryComp))
                return;

            if (!TryComp<MechChassisComponent>(comp.ContainerDict["chassis"].ContainedEntity, out var chassisComp))
                return;

            comp.NextPowerDrain = _timing.CurTime + new TimeSpan(0,0,1);

            var drainedEnergy = (comp.OverallMass * chassisComp.Efficiency);

            if (comp.OverallMass >= comp.MaximalMass * 2)
                drainedEnergy = comp.MaximalMass * 2 * chassisComp.Efficiency;

            _battery.ChangeCharge((EntityUid)batteryEnt, -(drainedEnergy).Float(), batteryComp);

            comp.Energy = batteryComp.CurrentCharge;

            Dirty(uid, comp);

            UpdateUserInterface(uid);
        }
    }

    private void OnChargeChanged(Entity<MechPartComponent> ent, ref ChargeChangedEvent args)
    {
        if(ent.Comp.PartOwner != null)
            UpdateMechOnlineStatus((EntityUid)ent.Comp.PartOwner, ent.Owner);
    }

    public void UpdateMechOnlineStatus(EntityUid mech, EntityUid battery)
    {
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Energy <= 0 && mechComp.Online)
        {
            mechComp.Online = false;

            TransferMindIntoPilot((mech,mechComp));

            if (mechComp.ContainerDict["chassis"].ContainedEntity != null)
            {
                _actionBlocker.UpdateCanMove(mech);
            }
        }
        if (mechComp.Energy > 0 && !mechComp.Online)
        {
            mechComp.Online = true;

            if(mechComp.PilotSlot.ContainedEntity != null)
                TransferMindIntoMech((mech, mechComp));

            if (mechComp.ContainerDict["chassis"].ContainedEntity != null)
            {
                _actionBlocker.UpdateCanMove(mech);
            }
        }
    }

}
