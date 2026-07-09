// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.SS220.Virology;
using Robust.Shared.Random;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    private const float MinAirbornePressure = Atmospherics.HazardLowPressure;

    private readonly List<(EntityUid Uid, VirusHolderComponent Holder)> _holderBuf = new();

    private void InitializeSpread()
    {
        SubscribeLocalEvent<VirusSusceptibleComponent, ContactInteractionEvent>(OnContact);
        SubscribeLocalEvent<VirusSusceptibleComponent, ReactionEntityEvent>(OnReaction);
        SubscribeLocalEvent<VirusProtectionComponent, InventoryRelayedEvent<VirusAddAttempt>>(OnProtection);
    }

    private void OnReaction(Entity<VirusSusceptibleComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        // a sealed suit will prevent infection
        if (IsVectorBlocked(ent.Owner, VirusTransmissionVector.Splash))
            return;

        InfectFromReagent(ent.Owner, args.ReagentQuantity.Reagent);
    }

    private void TickSpread()
    {
        _holderBuf.Clear();
        var query = EntityQueryEnumerator<VirusHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.Viruses.Count == 0)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            _holderBuf.Add((uid, holder));
        }

        foreach (var (uid, holder) in _holderBuf)
            TryTransmitProximity(uid, holder);
    }

    private void TryTransmitProximity(EntityUid source, VirusHolderComponent holder)
    {
        if (IsVectorBlocked(source, VirusTransmissionVector.Proximity))
            return;

        if (_atmos.GetContainingMixture(source) is not { } air || air.Pressure < MinAirbornePressure)
            return;

        var coords = _transform.GetMapCoordinates(source);

        foreach (var strain in EnumerateStrains(holder))
        {
            if (!TryGetActiveTransmission(strain.Comp, out var transmission) || transmission.ProximityChance <= 0f)
                continue;

            var descriptor = ToDescriptor(strain);

            foreach (var (target, _) in _lookup.GetEntitiesInRange<VirusSusceptibleComponent>(coords, transmission.ProximityRange))
            {
                if (target == source)
                    continue;

                if (!_random.Prob(transmission.ProximityChance))
                    continue;

                if (!_interaction.InRangeUnobstructed(source, target, transmission.ProximityRange))
                    continue;

                if (IsVectorBlocked(target, VirusTransmissionVector.Proximity))
                    continue;

                AddVirus(target, descriptor.Clone());
            }

            // airborne strains settle on nearby food/drink
            foreach (var (food, _) in _lookup.GetEntitiesInRange<EdibleComponent>(coords, transmission.ProximityRange))
            {
                if (_random.Prob(transmission.ProximityChance)
                    && _interaction.InRangeUnobstructed(source, food, transmission.ProximityRange))
                    Contaminate(food, descriptor);
            }
        }
    }

    private void OnContact(Entity<VirusSusceptibleComponent> ent, ref ContactInteractionEvent args)
    {
        // mob <-> mob: each infected side spreads to the other (runs from both parties, once per direction)
        if (HasComp<VirusSusceptibleComponent>(args.Other))
        {
            if (TryComp<VirusHolderComponent>(ent, out var holder))
                TryTransmitContact((ent.Owner, holder), args.Other);

            return;
        }

        // infected host leaves its contact-vector strains on a touched item
        if (TryComp<VirusHolderComponent>(ent, out var selfHolder)
            && !IsVectorBlocked(ent.Owner, VirusTransmissionVector.Contact))
        {
            foreach (var strain in EnumerateStrains(selfHolder))
            {
                if (TryGetActiveTransmission(strain.Comp, out var transmission) && transmission.ContactChance > 0f)
                    Contaminate(args.Other, ToDescriptor(strain));
            }
        }

        // contaminated item passes its strains
        if (TryComp<VirusContaminantComponent>(args.Other, out var contaminant)
            && !IsVectorBlocked(ent.Owner, VirusTransmissionVector.Contact))
        {
            foreach (var strain in contaminant.Viruses)
            {
                var descriptor = strain.Descriptor;
                if (descriptor.Transmission is { ContactChance: > 0f } transmission && _random.Prob(transmission.ContactChance))
                    AddVirus(ent.Owner, descriptor.Clone());
            }
        }
    }

    private void TryTransmitContact(Entity<VirusHolderComponent> source, EntityUid target)
    {
        if (IsVectorBlocked(source.Owner, VirusTransmissionVector.Contact)
            || IsVectorBlocked(target, VirusTransmissionVector.Contact))
            return;

        foreach (var strain in EnumerateStrains(source.Comp))
        {
            if (!TryGetActiveTransmission(strain.Comp, out var transmission) || transmission.ContactChance <= 0f)
                continue;

            if (_random.Prob(transmission.ContactChance))
                AddVirus(target, ToDescriptor(strain));
        }
    }

    private bool TryGetActiveTransmission(VirusComponent comp, out VirusTransmission transmission)
    {
        transmission = default!;
        if (comp.SuppressedUntil != null || IsBloodOnly(comp) || comp.Transmission is not { } profile)
            return false;

        transmission = profile;
        return true;
    }

    private bool IsVectorBlocked(EntityUid entity, VirusTransmissionVector vector)
    {
        var attempt = new VirusAddAttempt(entity, vector);
        RaiseLocalEvent(entity, ref attempt);
        return attempt.Cancelled;
    }

    private void OnProtection(Entity<VirusProtectionComponent> ent, ref InventoryRelayedEvent<VirusAddAttempt> args)
    {
        if (args.Args.Cancelled)
            return;

        if ((ent.Comp.Vectors & args.Args.Vector) == 0)
            return;

        if (_random.Prob(ent.Comp.BlockChance))
            args.Args.Cancelled = true;
    }
}
