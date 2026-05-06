using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class HitscanBlockSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null || args.Data.Shooter == null)
            return;

        var vector = _transform.GetWorldPosition((EntityUid)args.Data.HitEntity) - _transform.GetWorldPosition((EntityUid)args.Data.Shooter);

        var ev = new HitscanBlockAttemptEvent(ent.Comp.Damage, vector.ToAngle() - new Angle(Math.PI/2));

        RaiseLocalEvent((EntityUid)args.Data.HitEntity, ref ev);

        args.Cancelled = ev.CancelledHit;
    }

}
