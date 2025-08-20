using Content.Shared.Implants;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.FellDownThrowIgnore;

public sealed class FellDownThrowIgnoreSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FellDownThrowIgnoreComponent, FellDownThrowAttemptEvent>(OnFellDownAttempt);

        SubscribeLocalEvent<FellDownThrowIgnoreImplantComponent, ImplantImplantedEvent>(OnImplant);
        SubscribeLocalEvent<FellDownThrowIgnoreImplantComponent, EntGotRemovedFromContainerMessage>(OnRemoveImplant);

    }

    private void OnFellDownAttempt(Entity<FellDownThrowIgnoreComponent> ent, ref FellDownThrowAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnImplant(Entity<FellDownThrowIgnoreImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted == null)
            return;

        EnsureComp<FellDownThrowIgnoreComponent>(args.Implanted.Value);
    }

    private void OnRemoveImplant(Entity<FellDownThrowIgnoreImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RemCompDeferred<FellDownThrowIgnoreComponent>(args.Container.Owner);
    }
}
