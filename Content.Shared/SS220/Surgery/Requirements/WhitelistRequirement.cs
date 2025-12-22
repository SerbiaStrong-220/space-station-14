// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Surgery.Requirements;

[DataDefinition]
public sealed partial class WhitelistRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

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
        // No idea how we do it...
        return Loc.GetString($"surgery-requirement-whitelist");
    }
}
