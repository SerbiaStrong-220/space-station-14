// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class WhitelistRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        if (!uid.Value.Valid)
            return false;

        return entityManager.System<EntityWhitelistSystem>().IsWhitelistPass(Whitelist, uid.Value);
    }
}
