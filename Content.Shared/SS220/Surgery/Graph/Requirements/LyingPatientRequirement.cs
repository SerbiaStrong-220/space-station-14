// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Standing;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class LyingPatientRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public EntityWhitelist StrapWhitelist;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        if (uid is not { } patient)
            return true;

        if (entityManager.TryGetComponent<BuckleComponent>(patient, out var buckle) &&
            buckle.Buckled &&
            buckle.BuckledTo is { } strap)
        {
            return entityManager.System<EntityWhitelistSystem>().IsWhitelistPass(StrapWhitelist, strap);
        }

        return entityManager.System<StandingStateSystem>().IsDown(patient);
    }
}
