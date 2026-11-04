// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Mind.Filters;

/// <summary>
/// Blacklist for races.
/// </summary>
public sealed partial class SpeciesMindFilter : MindFilter
{
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Species = new();

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan)
    {
        var target = mind.Comp.OwnedEntity;

        if (target == null)
            return false;

        if (!entMan.TryGetComponent<HumanoidProfileComponent>(target.Value, out var profile))
            return false;

        return Species.Contains(profile.Species);
    }
}

