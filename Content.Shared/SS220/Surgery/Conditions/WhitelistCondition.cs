// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.Whitelist;
using JetBrains.Annotations;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class WhitelistCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    public bool Condition(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        return entityManager.System<EntityWhitelistSystem>().IsWhitelistPass(Whitelist, toolUid);
    }

    public string ConditionDescription()
    {
        return "ConditionDescriptionLocPath";
    }
}
