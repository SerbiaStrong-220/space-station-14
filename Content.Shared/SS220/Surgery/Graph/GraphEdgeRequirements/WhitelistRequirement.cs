// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Surgery.Graph.GraphEdgeRequirements;

[DataDefinition]
public sealed partial class WhitelistRequirement : SurgeryGraphEdgeRequirement
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    [DataField(required: true)]
    public LocId Description;

    public override bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        return entityManager.System<EntityWhitelistSystem>().IsWhitelistPass(Whitelist, toolUid);
    }

    public override bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(targetUid, toolUid, userUid, entityManager))
            return false;

        return true;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(Description);
    }
}
