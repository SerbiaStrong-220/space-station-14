using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Spawners;

namespace Content.Shared.SS220.BlockMove;

// TODO: Testing and fix
public sealed partial class BlockActionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BlockActionComponent, ComponentInit>(OnStartup);
        SubscribeLocalEvent<BlockActionComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<BlockActionComponent, UpdateCanMoveEvent>(OnAttemptMove);
        SubscribeLocalEvent<BlockActionComponent, ShotAttemptedEvent>(OnAttemptShoot);
        SubscribeLocalEvent<BlockActionComponent, AttemptMeleeEvent>(OnAttemptAttack);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BlockActionComponent>();

        while (query.MoveNext(out var target, out var comp))
        {
            switch (comp.Duration)
            {
                case null:
                    continue;
                case > 0:
                    comp.Duration -= frameTime;
                    break;
                default:
                    RemCompDeferred<BlockActionComponent>(target);
                    break;
            }
        }
    }

    private void OnStartup(Entity<BlockActionComponent> ent, ref ComponentInit args)
    {
        UpdateCanMove(ent, args);

        var effectEntity = Spawn(ent.Comp.BlockMoveEffectProto, Transform(ent).Coordinates);

        EnsureComp<TimedDespawnComponent>(effectEntity, out var despawnComponent);

        if (ent.Comp.Duration == null)
            return;

        despawnComponent.Lifetime = ent.Comp.Duration.Value;
    }

    private void OnRemove(Entity<BlockActionComponent> ent, ref ComponentRemove args)
    {
        UpdateCanMove(ent, args);
    }

    private void OnAttemptMove(Entity<BlockActionComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.BlockMove)
            args.Cancel();
    }

    private void OnAttemptShoot(Entity<BlockActionComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.BlockShoot)
            args.Cancel();
    }

    private void OnAttemptAttack(Entity<BlockActionComponent> ent, ref AttemptMeleeEvent args)
    {
        if (ent.Comp.BlockAttack)
            args.Cancelled = true;
    }

    private void UpdateCanMove(Entity<BlockActionComponent> ent, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(ent.Owner);
    }
}
