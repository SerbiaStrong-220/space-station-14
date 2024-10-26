// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Trackers.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Trackers.Systems;

public sealed class DamageReceivedTrackerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageReceivedTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [typeof(MobThresholdSystem)]);
    }

    private void OnDamageChanged(Entity<DamageReceivedTrackerComponent> entity, ref DamageChangedEvent args)
    {

        if (args.DamageDelta == null || !args.DamageIncreased
            || args.Origin != entity.Comp.WhomDamageTrack)
            return;

        if (entity.Comp.DamageTracker.AllowedState == null
            || !TryComp<MobStateComponent>(entity.Owner, out var mobState)
            || !entity.Comp.DamageTracker.AllowedState!.Contains(mobState.CurrentState))
            return;

        var damageGroup = _prototype.Index(entity.Comp.DamageTracker.DamageGroup);
        args.DamageDelta.TryGetDamageInGroup(damageGroup, out var trackableDamage);
        entity.Comp.CurrentAmount += trackableDamage;
    }
}
