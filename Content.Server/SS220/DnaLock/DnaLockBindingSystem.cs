// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.DnaLock;
using Content.Shared.SS220.DnaLock.Components;
using Robust.Shared.Containers;

namespace Content.Server.SS220.DnaLock;

public sealed partial class DnaLockBindingSystem : EntitySystem
{
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DnaLockSystem _dnaLock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DnaLockableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DnaLockableComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
    }

    // Items spawned after the job was assigned would otherwise stay unusable for everyone.
    private void OnMapInit(Entity<DnaLockableComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.RoundstartJob is not { } job)
            return;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (_job.MindHasJobWithId(mindId, job) && _dnaLock.TryGetDna(mind.OwnedEntity, out var dna))
                _dnaLock.AllowDna(ent, dna);
        }
    }

    private void OnInsertedIntoContainer(Entity<DnaLockableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!ent.Comp.BindToFirstHolder)
            return;

        ent.Comp.BindToFirstHolder = false;

        var holder = args.Container.Owner;
        string? dna;
        while (!_dnaLock.TryGetDna(holder, out dna))
        {
            if (!_container.TryGetContainingContainer(holder, out var container))
                return;

            holder = container.Owner;
        }

        _dnaLock.RegisterDna(ent, holder, dna);
    }

    private void OnRoleAdded(RoleAddedEvent ev)
    {
        if (!_job.MindTryGetJobId(ev.MindId, out var jobId) || !_dnaLock.TryGetDna(ev.Mind.OwnedEntity, out var dna))
            return;

        var query = EntityQueryEnumerator<DnaLockableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.RoundstartJob == jobId)
                _dnaLock.AllowDna((uid, comp), dna);
        }
    }
}
