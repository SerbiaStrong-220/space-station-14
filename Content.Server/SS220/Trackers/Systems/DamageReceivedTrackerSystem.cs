// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Trackers.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Trackers.Systems;

public sealed class DamageReceivedTrackerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const float ResetDamageOwnerDelaySeconds = 2.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageReceivedTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [typeof(MobThresholdSystem)]);
    }

    private void OnDamageChanged(Entity<DamageReceivedTrackerComponent> entity, ref DamageChangedEvent args)
    {

        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        if (args.Origin != entity.Comp.WhomDamageTrack
            && entity.Comp.ResetTimeDamageOwnerTracked < _gameTiming.CurTime)
        {
            Log.Debug($"Damage from {ToPrettyString(args.Origin)} wasnt counted due to not being {ToPrettyString(entity.Comp.WhomDamageTrack)}");
            return;
        }

        if (entity.Comp.DamageTracker.AllowedState != null
            && (!TryComp<MobStateComponent>(entity.Owner, out var mobState)
            || !entity.Comp.DamageTracker.AllowedState!.Contains(mobState.CurrentState)))
            return;

        Log.Debug($"Damage {ToPrettyString(entity)} received is tracked");
        var damageGroup = _prototype.Index(entity.Comp.DamageTracker.DamageGroup);
        args.DamageDelta.TryGetDamageInGroup(damageGroup, out var trackableDamage);
        entity.Comp.CurrentAmount += trackableDamage;

        entity.Comp.ResetTimeDamageOwnerTracked = _gameTiming.CurTime + TimeSpan.FromSeconds(ResetDamageOwnerDelaySeconds);
    }
}
