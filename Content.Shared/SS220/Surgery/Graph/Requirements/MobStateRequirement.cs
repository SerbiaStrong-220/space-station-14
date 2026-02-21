// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class MobStateRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public HashSet<MobState> States;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<MobStateComponent>(uid, out var mobStateComponent))
            return false;

        return States.Contains(mobStateComponent.CurrentState);
    }
}
