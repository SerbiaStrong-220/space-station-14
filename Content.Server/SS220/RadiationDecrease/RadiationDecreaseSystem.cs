using Content.Server.Power.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.SS220.RadiationDecrease;
using Robust.Shared.Timing;

namespace Content.Server.SS220.RadiationDecrease;

public sealed partial class RadiationDecreaseSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationDecreaseComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(EntityUid uid, RadiationDecreaseComponent comp, ComponentInit args)
    {
        if (TryComp<RadiationSourceComponent>(uid, out var radSourceComponent) && radSourceComponent.Intensity != 0)
        {
            comp.Intensity = radSourceComponent.Intensity;
        }
        if (TryComp<PowerSupplierComponent>(uid, out var supplyComp) && supplyComp.MaxSupply != 0)
        {
            comp.Supply = supplyComp.MaxSupply;
        }
    }

    public override void Update(float delta)
    {
        base.Update(delta);
        var query = EntityQueryEnumerator<RadiationDecreaseComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TryComp<RadiationSourceComponent>(uid, out var radSourceComponent) && radSourceComponent.Intensity != 0)
            {
                var decreasePerSecond = comp.Intensity / comp.TotalAliveTime;

                var curTime = _gameTiming.CurTime;

                if (curTime - comp.LastTimeDecreaseRad < comp.CoolDown)
                {
                    return;
                }

                comp.LastTimeDecreaseRad = curTime;
                if (radSourceComponent.Intensity - decreasePerSecond < 0) // without if - crash Pow3r
                {
                    radSourceComponent.Intensity = 0;
                    decreasePerSecond = 0;
                }
                radSourceComponent.Intensity -= decreasePerSecond;
            }

            if (TryComp<PowerSupplierComponent>(uid, out var powerSupply) && powerSupply.MaxSupply != 0)
            {
                var decreasePerSecond = comp.Supply / comp.TotalAliveTime;

                var curTime = _gameTiming.CurTime;

                if (curTime - comp.LastTimeDecreaseSupply < comp.CoolDown)
                {
                    return;
                }

                comp.LastTimeDecreaseSupply = curTime;
                if (powerSupply.MaxSupply - decreasePerSecond < 0) // without if - crash Pow3r
                {
                    powerSupply.MaxSupply = 0;
                    decreasePerSecond = 0;
                }
                powerSupply.MaxSupply -= decreasePerSecond;
            }
        }
    }
}
