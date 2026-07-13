// SS220 Changeling
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;

namespace Content.Server.Changeling.Mutations;

public sealed partial class ChangelingMutationSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    private void OnLastResort(Entity<ChangelingResourceComponent> ent, ref ChangelingLastResortActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !_mind.TryGetMind(ent.Owner, out var mind, out _) ||
            !HasComp<ChangelingIdentityComponent>(ent.Owner))
            return;

        var headslug = Spawn(args.HeadslugPrototype, Transform(ent).Coordinates);
        var headslugComp = EnsureComp<ChangelingHeadslugComponent>(headslug);
        headslugComp.AbandonedBody = ent.Owner;

        var storedState = Spawn(null, MapCoordinates.Nullspace);
        EnsureComp<ChangelingLastResortStorageComponent>(storedState);
        if (!CanTransferChangelingBody(ent, storedState) || !TrySpend(ent, args.ChemicalCost))
        {
            QueueDel(headslug);
            QueueDel(storedState);
            return;
        }

        if (!TransferChangelingBody(ent, storedState))
        {
            QueueDel(headslug);
            QueueDel(storedState);
            _resources.AddChemicals(ent.Owner, Content.Shared.FixedPoint.FixedPoint2.New(args.ChemicalCost));
            return;
        }

        args.Handled = true;
        headslugComp.StoredState = storedState;
        _mind.TransferTo(mind, headslug);

        _mobState.ChangeMobState(ent.Owner, MobState.Dead);
    }

    private void OnLayEgg(Entity<ChangelingHeadslugComponent> ent, ref ChangelingLayEggActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            ent.Comp.HasLaidEgg ||
            args.Target == ent.Owner ||
            !HasComp<HumanoidProfileComponent>(args.Target) ||
            !TryComp<MobStateComponent>(args.Target, out var targetMob) ||
            targetMob.CurrentState != MobState.Dead ||
            HasComp<ChangelingIncubatingEggComponent>(args.Target) ||
            args.HatchDelay < TimeSpan.Zero)
            return;

        args.Handled = true;
        var egg = EnsureComp<ChangelingIncubatingEggComponent>(args.Target);
        egg.Headslug = ent.Owner;
        egg.HatchAt = _timing.CurTime + args.HatchDelay;
        egg.HatchPrototype = args.HatchPrototype;
        ent.Comp.HasLaidEgg = true;

        // The mind remains on the dead headslug until hatching, preserving the
        // player's memory and allowing the egg to be destroyed during incubation.
        _mobState.ChangeMobState(ent.Owner, MobState.Dead);
    }

    private void UpdateEggs(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ChangelingIncubatingEggComponent>();
        while (query.MoveNext(out var corpse, out var egg))
        {
            if (egg.HatchAt > now)
                continue;

            if (TerminatingOrDeleted(egg.Headslug) ||
                !TryComp<ChangelingHeadslugComponent>(egg.Headslug, out var headslug) ||
                headslug.StoredState is not { } storedState ||
                TerminatingOrDeleted(storedState) ||
                !HasComp<ChangelingLastResortStorageComponent>(storedState) ||
                !TryComp<ChangelingResourceComponent>(storedState, out var resources) ||
                !_mind.TryGetMind(egg.Headslug, out var mind, out _))
            {
                RemCompDeferred<ChangelingIncubatingEggComponent>(corpse);
                continue;
            }

            var hatchling = Spawn(egg.HatchPrototype, Transform(corpse).Coordinates);
            if (!TransferChangelingBody((storedState, resources), hatchling))
            {
                QueueDel(hatchling);
                RemCompDeferred<ChangelingIncubatingEggComponent>(corpse);
                continue;
            }
            _mind.TransferTo(mind, hatchling);

            headslug.StoredState = null;
            _gibbing.Gib(corpse, user: hatchling);
            QueueDel(storedState);
            QueueDel(egg.Headslug);
        }
    }

    private void OnHeadslugShutdown(Entity<ChangelingHeadslugComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.StoredState is { } storedState && !TerminatingOrDeleted(storedState))
            QueueDel(storedState);
    }

    private void OnIncubatingEggShutdown(Entity<ChangelingIncubatingEggComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Comp.Headslug))
            QueueDel(ent.Comp.Headslug);
    }
}
