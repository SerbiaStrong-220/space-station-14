// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class HitscanBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null || args.Data.Shooter == null)
            return;

        var ev = new HitscanBlockAttemptEvent(ent.Comp.Damage, (EntityUid)args.Data.Shooter, ent.Owner);

        RaiseLocalEvent((EntityUid)args.Data.HitEntity, ref ev);

        args.Cancelled = ev.Cancelled;
    }

}
