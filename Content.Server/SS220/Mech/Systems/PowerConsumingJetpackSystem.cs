// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Mech.Systems;

public sealed partial class PowerConsumingJetpackSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AltMechSystem _mech = default!;
    [Dependency] private SharedJetpackSystem _jetpack = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PowerConsumingJetpackComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<ActiveJetpackComponent>(uid, out var activeComp) ||
                !TryComp<AltMechEquipmentComponent>(uid, out var equipmentComp) ||
                equipmentComp.EquipmentOwner is not { Valid: true } mechValid ||
                !TryComp<AltMechComponent>(equipmentComp.EquipmentOwner, out var mechComp) ||
                !TryComp<JetpackComponent>(uid, out var jetpackComp))
                continue;

            if (curTime < comp.NextPowerDrain)
                continue;

            comp.NextPowerDrain = _timing.CurTime + new TimeSpan(0, 0, 1);

            if (!_mech.TryChangeMechCharge((mechValid, mechComp), -comp.PowerConsumption))
                _jetpack.SetEnabled(uid, jetpackComp, false);

        }
    }

}
