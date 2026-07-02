// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class DogVitalitySystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogVitalityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DogVitalityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DogVitalityComponent, PathologySeverityChanged>(OnSeverityChanged);
        SubscribeLocalEvent<DogVitalityComponent, RefreshMobThresholdsModifiersEvent>(OnRefreshModifiers);
    }

    private void OnStartup(Entity<DogVitalityComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnShutdown(Entity<DogVitalityComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Reverting = true;
        Refresh(ent);
    }

    private void OnSeverityChanged(Entity<DogVitalityComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId == ent.Comp.Pathology)
            Refresh(ent);
    }

    private void OnRefreshModifiers(Entity<DogVitalityComponent> ent, ref RefreshMobThresholdsModifiersEvent args)
    {
        if (ent.Comp.Reverting || !TryGetThreshold(ent, out var value))
            return;

        args.ApplyModifier(MobState.Critical, new MobThresholdsModifier { Multiplier = 0, Flat = value });
        args.ApplyModifier(MobState.Dead, new MobThresholdsModifier { Multiplier = 0, Flat = value + ent.Comp.DeathThresholdOffset });
    }

    private void Refresh(Entity<DogVitalityComponent> ent)
    {
        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
            _mobThreshold.RefreshModifiers((ent.Owner, thresholds));
    }

    private bool TryGetThreshold(Entity<DogVitalityComponent> ent, out FixedPoint2 value)
    {
        return _pathology.TryGetStageValue(ent.Owner, ent.Comp.Pathology, ent.Comp.Thresholds, out value);
    }
}
