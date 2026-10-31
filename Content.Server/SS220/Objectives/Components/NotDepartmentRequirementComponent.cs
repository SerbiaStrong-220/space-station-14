// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Objectives.Components;

/// <summary>
/// Prevents an objective from being assigned to jobs in any of the listed departments.
/// </summary>
[RegisterComponent, Access(typeof(NotDepartmentRequirementSystem))]
public sealed partial class NotDepartmentRequirementComponent : Component
{
    /// <summary>
    /// Department prototype IDs whose jobs cannot receive this objective, including non-primary departments.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments = [];
}
