// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class OperatingComputerRequirement : SurgeryGraphRequirement
{
    [DataField]
    public ProtoId<SurgeryGraphPrototype>? RequiredSurgery;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        var surgerySystem = entityManager.System<SharedSurgerySystem>();
        return surgerySystem.HasLinkedOperatingComputer(uid.Value, RequiredSurgery);
    }
}
