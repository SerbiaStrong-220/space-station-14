// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class DiseaseSpreadSystem : EntitySystem
{
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private DiseaseContaminationSystem _contamination = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan ProximityInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextScan;
    private const float MinAirbornePressure = Atmospherics.HazardLowPressure;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyHolderComponent, ContactInteractionEvent>(OnContact);
        SubscribeLocalEvent<PathologyHolderComponent, ReactionEntityEvent>(OnReaction);
        SubscribeLocalEvent<DiseaseProtectionComponent, InventoryRelayedEvent<VirusAddedAttempt>>(OnProtection);
    }

    private void OnReaction(Entity<PathologyHolderComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        _pathology.InfectFromReagent(ent!, args.ReagentQuantity.Reagent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextScan)
            return;

        _nextScan = _timing.CurTime + ProximityInterval;

        var query = EntityQueryEnumerator<PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.ActiveViruses.Count == 0)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            TryTransmitProximity((uid, holder));
        }
    }

    private void TryTransmitProximity(Entity<PathologyHolderComponent> source)
    {
        // mask on host stops all "airborne" spread this tick
        // checked once here rather than once per virus
        if (IsVectorBlocked(source.Owner, VirusTransmissionVector.Proximity))
            return;

        if (_atmos.GetContainingMixture(source.Owner) is not { } air || air.Pressure < MinAirbornePressure)
            return;

        var coords = _transform.GetMapCoordinates(source.Owner);

        // spread only touches targets/items, never the source's own virus set, so iterate it directly
        foreach (var virus in source.Comp.ActiveViruses.Values)
        {
            if (virus.SuppressedUntil != null)
                continue;

            if (virus.Transmission is not { ProximityChance: > 0f } transmission)
                continue;

            foreach (var (target, _) in _lookup.GetEntitiesInRange<PathologyHolderComponent>(coords, transmission.ProximityRange))
            {
                if (target == source.Owner)
                    continue;

                if (!_random.Prob(transmission.ProximityChance))
                    continue;

                if (!_interaction.InRangeUnobstructed(source.Owner, target, transmission.ProximityRange))
                    continue;

                // target's gear also can block the infection
                if (IsVectorBlocked(target, VirusTransmissionVector.Proximity))
                    continue;

                _pathology.AddVirus(target, virus.Clone());
            }

            // "airborne" strains also settle on nearby food/drink, eating it later infects
            foreach (var (food, _) in _lookup.GetEntitiesInRange<EdibleComponent>(coords, transmission.ProximityRange))
            {
                if (_random.Prob(transmission.ProximityChance)
                    && _interaction.InRangeUnobstructed(source.Owner, food, transmission.ProximityRange))
                    _contamination.Contaminate(food, virus);
            }
        }
    }

    private void OnContact(Entity<PathologyHolderComponent> ent, ref ContactInteractionEvent args)
    {
        if (HasComp<PathologyHolderComponent>(args.Other))
        {
            // host <-> host contact runs once so no duplicate.
            TryTransmitContact((ent.Owner, ent.Comp), args.Other);
            return;
        }

        // host <-> item bare hand both leaves and picks up contamination
        if (IsVectorBlocked(ent.Owner, VirusTransmissionVector.Contact))
            return;

        // host -> item leaves the host's contact-vector strains on item
        foreach (var virus in ent.Comp.ActiveViruses.Values)
        {
            if (virus.SuppressedUntil == null && virus.Transmission is { ContactChance: > 0f })
                _contamination.Contaminate(args.Other, virus);
        }

        // item -> host catch what the item carries, by strain's contact chance
        if (TryComp<VirusContaminationComponent>(args.Other, out var contamination))
        {
            foreach (var virus in contamination.Viruses)
            {
                if (virus.Transmission is { ContactChance: > 0f } transmission && _random.Prob(transmission.ContactChance))
                    _pathology.AddVirus(ent!, virus.Clone());
            }
        }
    }

    private void TryTransmitContact(Entity<PathologyHolderComponent?> source, Entity<PathologyHolderComponent?> target)
    {
        if (!Resolve(source.Owner, ref source.Comp, false) || !Resolve(target.Owner, ref target.Comp, false))
            return;

        if (source.Comp.ActiveViruses.Count == 0)
            return;

        // spread only touches the target, never the source's own virus set, so iterate it directly
        foreach (var virus in source.Comp.ActiveViruses.Values)
        {
            // a suppressed strain is not contagious
            if (virus.SuppressedUntil != null)
                continue;

            if (virus.Transmission is not { ContactChance: > 0f } transmission)
                continue;

            if (!_random.Prob(transmission.ContactChance))
                continue;

            // host's gear can contain spread, target's gear can also block infection
            if (IsVectorBlocked(source.Owner, VirusTransmissionVector.Contact)
                || IsVectorBlocked(target.Owner, VirusTransmissionVector.Contact))
                continue;

            _pathology.AddVirus(target, virus.Clone());
        }
    }

    private bool IsVectorBlocked(EntityUid entity, VirusTransmissionVector vector)
    {
        var attempt = new VirusAddedAttempt(entity, vector);
        RaiseLocalEvent(entity, ref attempt);
        return attempt.Cancelled;
    }

    private void OnProtection(Entity<DiseaseProtectionComponent> ent, ref InventoryRelayedEvent<VirusAddedAttempt> args)
    {
        if (args.Args.Cancelled)
            return;

        if ((ent.Comp.Vectors & args.Args.Vector) == 0)
            return;

        if (_random.Prob(ent.Comp.BlockChance))
            args.Args.Cancelled = true;
    }
}
