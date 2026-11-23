// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Traits;
using Content.Shared.Body;
using Content.Shared.GameTicking;
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

    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(TraitSystem)]);
        SubscribeLocalEvent<CongenitalTraitsComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<CongenitalTraitsComponent, OrganGotInsertedEvent>(OnInserted);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!_body.TryGetOrganByCategory(args.Mob, BrainCategory, out var brain))
            return;

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!IsBrainTrait(traitId)
                || GetTraitComponents(traitId) is not { } components
                // the trait system skips a trait the job or the species doesn't allow
                || !HasAllComponents(args.Mob, components))
                continue;

            var congenital = EnsureComp<CongenitalTraitsComponent>(brain.Value);
            congenital.Traits.Add(traitId);
            Dirty(brain.Value, congenital);
        }
    }

    private void OnRemoved(Entity<CongenitalTraitsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        foreach (var traitId in ent.Comp.Traits)
        {
            RemoveTraitComponents(args.Target, traitId);
        }
    }

    private void OnInserted(Entity<CongenitalTraitsComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var traitId in ent.Comp.Traits)
        {
            if (GetTraitComponents(traitId) is { } components)
                EntityManager.AddComponents(args.Target, components, removeExisting: false);
        }
    }

    /// <summary>
    /// Cures a random congenital trait of the body's brain.
    /// </summary>
    public bool TryCureRandomTrait(EntityUid body)
    {
        if (!_body.TryGetOrganByCategory(body, BrainCategory, out var brain)
            || !TryComp<CongenitalTraitsComponent>(brain, out var congenital)
            || congenital.Traits.Count == 0)
            return false;

        var traitId = _random.Pick(congenital.Traits);
        congenital.Traits.Remove(traitId);
        Dirty(brain.Value, congenital);
        RemoveTraitComponents(body, traitId);
        return true;
    }

    private void RemoveTraitComponents(EntityUid body, ProtoId<TraitPrototype> traitId)
    {
        // an acquired pathology with the same trait keeps it
        if (_pathology.IsTraitInUse(body, traitId) || GetTraitComponents(traitId) is not { } components)
            return;

        EntityManager.RemoveComponents(body, components);
    }

    private bool IsBrainTrait(ProtoId<TraitPrototype> traitId)
    {
        foreach (var pathology in _prototype.EnumeratePrototypes<PathologyPrototype>())
        {
            if (pathology.Organ != BrainCategory)
                continue;

            foreach (var definition in pathology.Definition)
            {
                if (definition.Trait == traitId)
                    return true;
            }
        }

        return false;
    }

    private ComponentRegistry? GetTraitComponents(ProtoId<TraitPrototype> traitId)
    {
        return _prototype.Resolve(traitId, out var trait) ? trait.Components : null;
    }

    private bool HasAllComponents(EntityUid uid, ComponentRegistry components)
    {
        foreach (var entry in components.Values)
        {
            if (!HasComp(uid, entry.Component.GetType()))
                return false;
        }

        return true;
    }
}
