// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusMetabolismSlowSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusMetabolismSlowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusMetabolismSlowComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusMetabolismSlowComponent, GetMetabolicMultiplierEvent>(OnGetMultiplier);
    }

    private void OnStartup(Entity<VirusMetabolismSlowComponent> ent, ref ComponentStartup args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnShutdown(Entity<VirusMetabolismSlowComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        ent.Comp.Reverting = true;
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnGetMultiplier(Entity<VirusMetabolismSlowComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (ent.Comp.Reverting || ent.Comp.Reduction <= 0f || ent.Comp.Reduction >= 1f)
            return;

        // higher multiplier = longer update interval = slower metabolism
        args.Multiplier *= 1f / (1f - ent.Comp.Reduction);
    }
}
