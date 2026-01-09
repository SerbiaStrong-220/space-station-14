// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class WhitelistRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    public override bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return false;

        return entityManager.System<EntityWhitelistSystem>().IsWhitelistPass(Whitelist, uid.Value);
    }

    public override bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(uid, entityManager))
            return false;

        return true;
    }
}
