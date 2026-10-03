using Content.Server.SS220.Bed.Cryostorage;
using Content.Shared.Gibbing;
using Content.Shared.Guardian.Components;
using Content.Shared.Hands.Components;

namespace Content.Server.Guardian;

// SS220 fix-Fatal-error-on-host-cryo: cryostorage pauses the host without deleting it,
// so ComponentShutdown never fires for GuardianHostComponent. Clean up the hosted
// guardian manually to avoid a dangling guardian/container reference.
public sealed class GuardianCryoFixSystem : EntitySystem
{
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianHostComponent, BeingCryoDeletedEvent>(OnCryoDeleted);
    }

    private void OnCryoDeleted(Entity<GuardianHostComponent> ent, ref BeingCryoDeletedEvent args)
    {
        if (ent.Comp.HostedGuardian is not { } guardian)
            return;

        if (HasComp<HandsComponent>(guardian))
            _gibbing.Gib(guardian);

        QueueDel(guardian);
        ent.Comp.HostedGuardian = null;
        QueueDel(ent.Comp.ActionEntity);
        ent.Comp.ActionEntity = null;
        Dirty(ent);
    }
}
