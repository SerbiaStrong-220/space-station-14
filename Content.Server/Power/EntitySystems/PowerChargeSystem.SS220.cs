using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerChargeSystem : EntitySystem
{
    public void ClearCharge(Entity<PowerChargeComponent> entity, EntityUid? origin = null)
    {
        entity.Comp.Charge = 0f;
        SetSwitchedOn(entity.Owner, entity.Comp, false, user: origin);
    }

    public void SetSwitchOn(Entity<PowerChargeComponent> entity, EntityUid? origin = null)
    {
        SetSwitchedOn(entity.Owner, entity.Comp, true, user: origin);
    }
}
