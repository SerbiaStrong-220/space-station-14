// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusHyperabsorptionSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusHyperabsorptionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusHyperabsorptionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusHyperabsorptionComponent, GetMetabolicMultiplierEvent>(OnGetMultiplier);
    }

    private void OnStartup(Entity<VirusHyperabsorptionComponent> ent, ref ComponentStartup args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnShutdown(Entity<VirusHyperabsorptionComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        ent.Comp.Reverting = true;
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnGetMultiplier(Entity<VirusHyperabsorptionComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (ent.Comp.Reverting || ent.Comp.SpeedBonus <= 0f)
            return;

        // lower multiplier = shorter update interval = faster metabolism
        args.Multiplier *= 1f / (1f + ent.Comp.SpeedBonus);
    }
}
