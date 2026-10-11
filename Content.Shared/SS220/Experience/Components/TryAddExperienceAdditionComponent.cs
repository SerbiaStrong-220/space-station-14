// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Used to change experience addition for logic which adds component by removing previous version
/// </summary>
[RegisterComponent]
[Access(friends: [typeof(ExperienceAdditionSystem)], Other = AccessPermissions.None)]
public sealed partial class TryAddExperienceAdditionComponent : Component
{
    [AlwaysPushInheritance]
    [DataField]
    public Dictionary<ExperienceAddSource, ExperienceAddSpecifier> Sources = new();

    [AlwaysPushInheritance]
    [DataField]
    public ProtoId<ExperienceDefinitionPrototype>? DefinitionId;
}
