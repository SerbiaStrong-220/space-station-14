// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.SS220.Virology.Behaviors;

public sealed partial class VirusDogVitalitySystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusDogVitalityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusDogVitalityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusDogVitalityComponent, RefreshMobThresholdsModifiersEvent>(OnRefreshModifiers);
    }

    private void OnStartup(Entity<VirusDogVitalityComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnShutdown(Entity<VirusDogVitalityComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Reverting = true;
        Refresh(ent);
    }

    private void OnRefreshModifiers(Entity<VirusDogVitalityComponent> ent, ref RefreshMobThresholdsModifiersEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        args.ApplyModifier(MobState.Critical, new MobThresholdsModifier { Flat = ent.Comp.Threshold, Compatible = true });
        args.ApplyModifier(MobState.Dead, new MobThresholdsModifier { Flat = ent.Comp.Threshold + ent.Comp.DeathThresholdOffset, Compatible = true });
    }

    private void Refresh(Entity<VirusDogVitalityComponent> ent)
    {
        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
            _mobThreshold.RefreshModifiers((ent.Owner, thresholds));
    }
}
