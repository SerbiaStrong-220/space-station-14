// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.Metabolism;
using Content.Shared.Rejuvenate;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public sealed partial class InitialBodySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";

    private void InitializeSurgery()
    {
        SubscribeLocalEvent<InitialBodyComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// Whether the species of the body has organs of this category, bodies without initial organs accept any.
    /// </summary>
    public bool IsSpeciesOrganCategory(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        return !TryComp<InitialBodyComponent>(body, out var initialBody) || initialBody.Organs.ContainsKey(category);
    }

    /// <summary>
    /// An organ fits the body when the species has its category and the organ metabolizes the same stages as the species' own one,
    /// e.g. a slime core is a brain that also digests and filters blood, so it can't replace a human brain and vice versa.
    /// </summary>
    public bool IsOrganCompatible(EntityUid body, Entity<OrganComponent?> organ)
    {
        if (!Resolve(organ, ref organ.Comp, logMissing: false) || organ.Comp.Category is not { } category)
            return false;

        if (!TryComp<InitialBodyComponent>(body, out var initialBody))
            return true;

        if (!initialBody.Organs.TryGetValue(category, out var speciesOrganId)
            || !_prototype.TryIndex(speciesOrganId.Id, out var speciesOrgan))
            return false;

        speciesOrgan.TryGetComponent<MetabolizerComponent>(out var speciesMetabolizer, _componentFactory);
        TryComp<MetabolizerComponent>(organ, out var metabolizer);

        return HaveSameStages(metabolizer?.Stages, speciesMetabolizer?.Stages);
    }

    private static bool HaveSameStages(List<ProtoId<MetabolismStagePrototype>>? first, List<ProtoId<MetabolismStagePrototype>>? second)
    {
        var count = first?.Count ?? 0;
        if (count != (second?.Count ?? 0))
            return false;

        for (var i = 0; i < count; i++)
        {
            if (!second!.Contains(first![i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Rejuvenate brings back the organs the body spawned with: missing ones are spawned, transplanted ones are swapped back.
    /// </summary>
    private void OnRejuvenate(Entity<InitialBodyComponent> ent, ref RejuvenateEvent args)
    {
        if (_net.IsClient || !TryComp<BodyComponent>(ent, out var body))
            return;

        var profile = GetBodyProfile(body);
        var restoredProfiles = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>();
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (category, organProto) in ent.Comp.Organs)
        {
            if (_body.TryGetOrganByCategory((ent, body), category, out var organ))
            {
                // a transplanted brain carries someone's mind, deleting it would kill that player
                if (category == BrainCategory || MetaData(organ.Value).EntityPrototype?.ID == organProto.Id)
                    continue;

                if (!_body.TryExtractOrgan((ent, body), category, out _))
                    continue;

                Del(organ.Value);
            }

            var spawned = Spawn(organProto, coords);
            if (!_body.TryInsertOrgan((ent, body), spawned))
            {
                Del(spawned);
                continue;
            }

            if (profile is { } organProfile)
                restoredProfiles[category] = organProfile;
        }

        // new visual organs (e.g. eyes) must get the body's colors instead of the prototype defaults
        if (restoredProfiles.Count > 0)
            _visualBody.ApplyProfiles(ent, restoredProfiles);
    }

    private OrganProfileData? GetBodyProfile(BodyComponent body)
    {
        if (body.Organs is null)
            return null;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
                return visualOrgan.Profile;
        }

        return null;
    }
}
