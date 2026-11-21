using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;

namespace Content.Server.SS220.RedWings;

[RegisterComponent]
public sealed partial class RedWingsClientPaperComponent : Component
{
    private const int DefaultClientAmount = 4;
    private static readonly ProtoId<DepartmentPrototype>[] DefaultForbiddenDepartment = ["Command", "Security"];
    private static readonly ProtoId<SpeciesPrototype>[] DefaultForbiddenSpecies = ["Ipc"];

    [DataField]
    public int ClientAmount = DefaultClientAmount;

    [DataField]
    public ProtoId<DepartmentPrototype>[] ForbiddenDepartment = DefaultForbiddenDepartment;

    [DataField]
    public ProtoId<SpeciesPrototype>[] ForbiddenSpecies = DefaultForbiddenSpecies;
}