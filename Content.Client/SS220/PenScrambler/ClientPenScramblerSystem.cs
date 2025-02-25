using Content.Shared.SS220.PenScrambler;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.PenScrambler;

public sealed class ClientPenScramblerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<SetScaleFromTargetEvent>(OnSetScaleFromTarget);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<SetScaleFromTargetComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp is { Target: not null, IsUpdated: false })
            {
                TryUpdateSprite((uid, comp));
            }
        }
    }

    private void OnSetScaleFromTarget(SetScaleFromTargetEvent args)
    {
        var owner = GetEntity(args.Owner);

        if (!TryComp<SetScaleFromTargetComponent>(owner, out var comp))
            return;

        comp.Target = args.Target;
        Dirty(owner, comp);

        TryUpdateSprite((owner, comp));
    }

    private void TryUpdateSprite(Entity<SetScaleFromTargetComponent> ent)
    {
        if (!ent.Comp.Target.HasValue)
            return;

        if (!EntityManager.TryGetEntity(ent.Comp.Target.Value, out var target))
            return;

        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteUser))
            return;

        if (!TryComp<SpriteComponent>(target, out var spriteTarget))
            return;

        spriteUser.Scale = spriteTarget.Scale;
        ent.Comp.IsUpdated = true;

        _sprite.QueueUpdateInert(ent.Owner, spriteUser);
    }
}
