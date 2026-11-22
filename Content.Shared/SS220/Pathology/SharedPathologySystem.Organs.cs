// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.SS220.Body;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private InitialBodySystem _initialBody = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;

    private void InitializeOrganEvents()
    {
        SubscribeLocalEvent<PathologyHolderComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<PathologyHolderComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<PathologyHolderComponent, PathologyAddedAttempt>(OnOrganPathologyAddAttempt);
        SubscribeLocalEvent<PathologyHolderComponent, PathologyRemoveAttempt>(OnOrganPathologyRemoveAttempt);
    }

    private void OnOrganRemoved(Entity<PathologyHolderComponent> entity, ref OrganRemovedFromEvent args)
    {
        // pathologies are server authoritative, client only receives container changes
        if (_net.IsClient || TerminatingOrDeleted(entity))
            return;

        if (!TryComp<OrganComponent>(args.Organ, out var organ) || organ.Category is not { } category)
            return;

        var organDeleted = TerminatingOrDeleted(args.Organ);
        var organPathologies = new ValueList<ProtoId<PathologyPrototype>>();
        foreach (var pathologyId in entity.Comp.ActivePathologies.Keys)
        {
            if (_prototype.Resolve(pathologyId, out var pathologyPrototype) && pathologyPrototype.Organ == category)
                organPathologies.Add(pathologyId);
        }

        OrganPathologyStorageComponent? storage = null;
        foreach (var pathologyId in organPathologies)
        {
            var pathologyPrototype = _prototype.Index(pathologyId);
            var data = entity.Comp.ActivePathologies[pathologyId];
            RemovePathology(entity, pathologyPrototype, applyContexts: false);

            if (organDeleted)
                continue;

            storage ??= EnsureComp<OrganPathologyStorageComponent>(args.Organ);
            storage.Pathologies[pathologyId] = data;
        }

        var rotten = !organDeleted && _rotting.IsRotten(entity);
        var speciesOrgan = _initialBody.IsSpeciesOrganCategory(entity, category);
        foreach (var pathologyPrototype in _prototype.EnumeratePrototypes<PathologyPrototype>())
        {
            if (speciesOrgan && pathologyPrototype.MissingOrgan == category)
                TryAddPathology(entity.AsNullable(), pathologyPrototype.ID);

            if (!rotten || !pathologyPrototype.FailsOnRot || pathologyPrototype.Organ != category)
                continue;

            storage ??= EnsureComp<OrganPathologyStorageComponent>(args.Organ);
            storage.Pathologies[pathologyPrototype.ID] = new PathologyInstanceData(_gameTiming.CurTime, null)
            {
                Level = pathologyPrototype.Definition.Length - 1,
            };
        }
    }

    private void OnOrganInserted(Entity<PathologyHolderComponent> entity, ref OrganInsertedIntoEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<OrganComponent>(args.Organ, out var organ) || organ.Category is not { } category)
            return;

        // bodies get all their organs inserted on spawn, there is nothing to cure then
        if (entity.Comp.ActivePathologies.Count > 0)
        {
            foreach (var pathologyPrototype in _prototype.EnumeratePrototypes<PathologyPrototype>())
            {
                if (pathologyPrototype.MissingOrgan == category)
                    TryRemovePathology(entity.AsNullable(), pathologyPrototype.ID, checkStacks: false);
            }
        }

        if (!TryComp<OrganPathologyStorageComponent>(args.Organ, out var storage))
            return;

        foreach (var (pathologyId, data) in storage.Pathologies)
        {
            TryRestorePathology(entity.AsNullable(), pathologyId, data);
        }

        RemComp(args.Organ, storage);
    }

    private void OnOrganPathologyAddAttempt(Entity<PathologyHolderComponent> entity, ref PathologyAddedAttempt args)
    {
        if (args.Cancelled || !TryComp<BodyComponent>(entity, out var body) || !_prototype.Resolve(args.PathologyId, out var pathologyPrototype))
            return;

        if (pathologyPrototype.AffectsBones && HasComp<BonelessComponent>(entity))
            args.Cancelled = true;
        // a prosthesis gets no wounds of the organ it replaces
        else if (pathologyPrototype.Organ is { } organ
                 && (!_body.TryGetOrganByCategory((entity, body), organ, out var installed) || HasComp<ProsthesisComponent>(installed)))
            args.Cancelled = true;
        else if (pathologyPrototype.MissingOrgan is { } missingOrgan && _body.HasOrganCategory((entity, body), missingOrgan))
            args.Cancelled = true;
    }

    private void OnOrganPathologyRemoveAttempt(Entity<PathologyHolderComponent> entity, ref PathologyRemoveAttempt args)
    {
        if (args.Cancelled || !TryComp<BodyComponent>(entity, out var body) || !_prototype.Resolve(args.PathologyId, out var pathologyPrototype))
            return;

        // absence of an organ is cured only by inserting one
        if (pathologyPrototype.MissingOrgan is { } missingOrgan && !_body.HasOrganCategory((entity, body), missingOrgan))
            args.Cancelled = true;
    }
}
