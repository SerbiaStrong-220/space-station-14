using Content.Shared.Movement.Components;
using Content.Shared.Power.Components;
using Content.Shared.SS220.Mech.Components;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
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

            _battery.ChangeCharge((EntityUid)batteryEnt, -(drainedEnergy).Float(),batteryComp);

            Dirty(uid, comp);

            UpdateUserInterface(uid);
        }
    }


}
