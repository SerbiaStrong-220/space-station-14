// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Surgery.Graph.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class WhitelistCondition : IAbstractSurgeryGraphAvailabilityCondition
{
    [DataField(required: true)]
    public FlippingCondition<EntityWhitelist> FlippingCondition;

    [DataField(required: true)]
    public string FailReasonPath = "code-issue-condition";

    public bool Condition(EntityUid uid, IEntityManager entityManager, [NotNullWhen(false)] out string? reason)
    {
        var whitelist = entityManager.System<EntityWhitelistSystem>();
        reason = Loc.GetString(FailReasonPath);

        return FlippingCondition.IsPassed((x) => whitelist.IsWhitelistPass(x, uid),
                                            (x) => whitelist.IsWhitelistFail(x, uid));

    }
}

public enum WhitelistCheckTypes
{
    Whitelist,
    Blacklist
}
