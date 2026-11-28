// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Traits;
using Content.Shared.Body;
using Content.Shared.Random;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Body;

/// <summary>
/// Moves the lobby traits of the brain together with it. A trait counts as a brain one when a brain pathology gives the same trait,
/// so the acquired and the congenital form of it live in the same organ.
/// </summary>
public sealed partial class CongenitalTraitSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, TraitAppliedEvent>(OnTraitApplied);
        SubscribeLocalEvent<CongenitalTraitsComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<CongenitalTraitsComponent, OrganGotInsertedEvent>(OnInserted);
    }

    private void OnTraitApplied(Entity<BodyComponent> ent, ref TraitAppliedEvent args)
    {
        if (!IsBrainTrait(args.Trait)
            || !_body.TryGetOrganByCategory(ent.Owner, InitialBodySystem.BrainCategory, out var brain))
            return;

        var congenital = EnsureComp<CongenitalTraitsComponent>(brain.Value);
        congenital.Traits.Add(args.Trait);
        Dirty(brain.Value, congenital);
    }

    private void OnRemoved(Entity<CongenitalTraitsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        foreach (var traitId in ent.Comp.Traits)
        {
            RemoveTraitEffects(args.Target, traitId);
        }
    }

    private void OnInserted(Entity<CongenitalTraitsComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var traitId in ent.Comp.Traits)
        {
            if (_prototype.Resolve(traitId, out var trait))
                _pathology.ApplyTraitEffects(args.Target, trait);
        }
    }

    /// <summary>
    /// Cures a random congenital trait of the body's brain.
    /// </summary>
    public bool TryCureRandomTrait(EntityUid body)
    {
        if (!_body.TryGetOrganByCategory(body, InitialBodySystem.BrainCategory, out var brain)
            || !TryComp<CongenitalTraitsComponent>(brain, out var congenital)
            || congenital.Traits.Count == 0)
            return false;

        var traitId = _random.Pick(congenital.Traits);
        congenital.Traits.Remove(traitId);
        Dirty(brain.Value, congenital);
        RemoveTraitEffects(body, traitId);
        return true;
    }

    /// <summary>
    /// The outcome of a successful lobotomy roll: cures a random congenital trait of the brain or, by bad luck, leaves a new brain trauma.
    /// </summary>
    public void Lobotomize(EntityUid body, float traumaChance, ProtoId<WeightedRandomPrototype> traumas)
    {
        if (_random.Prob(traumaChance))
        {
            _pathology.TryAddRandom(body, traumas, 1f);
            return;
        }

        TryCureRandomTrait(body);
    }

    private void RemoveTraitEffects(EntityUid body, ProtoId<TraitPrototype> traitId)
    {
        // an acquired pathology with the same trait keeps it
        if (_pathology.IsTraitInUse(body, traitId) || !_prototype.Resolve(traitId, out var trait))
            return;

        _pathology.RemoveTraitEffects(body, trait);
    }

    private bool IsBrainTrait(ProtoId<TraitPrototype> traitId)
    {
        foreach (var pathology in _prototype.EnumeratePrototypes<PathologyPrototype>())
        {
            if (pathology.Organ != InitialBodySystem.BrainCategory)
                continue;

            foreach (var definition in pathology.Definition)
            {
                if (definition.Trait == traitId)
                    return true;
            }
        }

        return false;
    }
}
