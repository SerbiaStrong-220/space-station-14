// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed class HyperabsorptionSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyperabsorptionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HyperabsorptionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HyperabsorptionComponent, PathologySeverityChanged>(OnSeverityChanged);
        SubscribeLocalEvent<HyperabsorptionComponent, GetMetabolicMultiplierEvent>(OnGetMultiplier);
    }

    private void OnStartup(Entity<HyperabsorptionComponent> ent, ref ComponentStartup args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnShutdown(Entity<HyperabsorptionComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        ent.Comp.Reverting = true;
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnSeverityChanged(Entity<HyperabsorptionComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId == ent.Comp.Pathology)
            _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnGetMultiplier(Entity<HyperabsorptionComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        if (!_pathology.TryGetStageValue(ent.Owner, ent.Comp.Pathology, ent.Comp.SpeedBonus, out var bonus))
            return;

        // lower multiplier = shorter update interval = faster metabolism
        args.Multiplier *= 1f / (1f + bonus);
    }
}
