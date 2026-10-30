// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's objectives.
/// </summary>
public sealed partial class BuyerObjectiveCondition : ListingCondition
{
    /// <summary>
    /// List of objectives that satisfy the condition.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ObjectiveComponent>> Objectives = new();

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return false;

        var mindSystem = ent.System<SharedMindSystem>();

        foreach (var objective in Objectives)
        {
            if (mindSystem.TryFindObjective(args.Buyer, objective.Id, out _))
                return true;
        }

        return false;
    }
}
