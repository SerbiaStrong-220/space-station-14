// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Body;
using Content.Shared.Random;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

/// <summary>
/// Runs on a successful roll of the step: cures a random congenital trait of the brain or, by bad luck, leaves a new brain trauma.
/// The failure of the roll itself is the failure of the lobotomy, so the risk of the step lowers the cure chance.
/// </summary>
[DataDefinition]
public sealed partial class LobotomyAction : ISurgeryGraphEdgeAction
{
    /// <summary>
    /// Chance of a new trauma instead of the cure among the successful rolls.
    /// </summary>
    [DataField]
    public float TraumaChance = 0.2f;

    [DataField]
    public ProtoId<WeightedRandomPrototype> Traumas = "BrainTraumaPathology";

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        entityManager.System<CongenitalTraitSystem>().Lobotomize(targetUid, TraumaChance, Traumas);
    }
}
