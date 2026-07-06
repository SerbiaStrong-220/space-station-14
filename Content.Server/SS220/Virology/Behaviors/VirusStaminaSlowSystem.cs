// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Components;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusStaminaSlowSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusStaminaSlowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusStaminaSlowComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<VirusStaminaSlowComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return;

        ent.Comp.OriginalDecay = stamina.Decay;
        ent.Comp.Applied = true;
        stamina.Decay *= Math.Clamp(1f - ent.Comp.SlowFraction, 0f, 1f);
        Dirty(ent.Owner, stamina);
    }

    private void OnShutdown(Entity<VirusStaminaSlowComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.Applied || !TryComp<StaminaComponent>(ent, out var stamina))
            return;

        stamina.Decay = ent.Comp.OriginalDecay;
        Dirty(ent.Owner, stamina);
    }
}
