// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Systems;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class OperatingComputerRequirement : SurgeryGraphRequirement
{
    /// <summary>
    /// The protocol the disk in the computer must have. As a visibility requirement it hides a step without the disk.
    /// Without it only a linked computer is needed.
    /// </summary>
    [DataField]
    public string? Protocol;

    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        var surgerySystem = entityManager.System<SharedSurgerySystem>();
        return surgerySystem.HasLinkedOperatingComputer(uid.Value, Protocol);
    }
}
