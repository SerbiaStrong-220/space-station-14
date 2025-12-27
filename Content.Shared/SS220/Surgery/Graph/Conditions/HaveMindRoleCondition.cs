// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Surgery.Systems;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Surgery.Graph.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class HaveMindRoleCondition : IAbstractSurgeryGraphAvailabilityCondition
{
    [DataField(required: true)]
    public FlippingCondition<string> FlippingCondition;

    [DataField]
    public string FailReasonPath = "code-issue-condition";

    public bool Condition(EntityUid uid, IEntityManager entityManager, [NotNullWhen(false)] out string? reason)
    {
        var roleSystem = entityManager.System<SharedRoleSystem>();
        var mindSystem = entityManager.System<SharedMindSystem>();
        var factory = entityManager.ComponentFactory;

        reason = Loc.GetString(FailReasonPath);
        var componentType = factory.GetRegistration(FlippingCondition.Value).Type;
        var mind = mindSystem.GetMind(uid);
        if (componentType is null)
        {
            entityManager.System<SharedSurgerySystem>().Log.Error($"Incorrect component name given, name which was provided is {FlippingCondition.Value}");
            return false;
        }

        if (mind is null)
        {
            reason = Loc.GetString("mind-role-condition-target-no-mind");
            return false;
        }

        return FlippingCondition.IsPassed(
            (_) => roleSystem.MindHasRole(mind.Value, componentType!, out var _),
            (_) => !roleSystem.MindHasRole(mind.Value, componentType!, out var _));
    }
}
