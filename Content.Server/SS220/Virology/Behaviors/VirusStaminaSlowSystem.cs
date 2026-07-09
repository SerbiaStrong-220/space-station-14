// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusStaminaSlowSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusStaminaSlowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusStaminaSlowComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusStaminaSlowComponent, RefreshStaminaDecayEvent>(OnRefreshDecay);
    }

    private void OnRefreshDecay(Entity<VirusStaminaSlowComponent> ent, ref RefreshStaminaDecayEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        args.Modifier *= Math.Clamp(1f - ent.Comp.SlowFraction, 0f, 1f);
    }

    private void OnStartup(Entity<VirusStaminaSlowComponent> ent, ref ComponentStartup args)
    {
        _stamina.RefreshStaminaDecay(ent.Owner);
    }

    private void OnShutdown(Entity<VirusStaminaSlowComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        ent.Comp.Reverting = true;
        _stamina.RefreshStaminaDecay(ent.Owner);
    }
}
