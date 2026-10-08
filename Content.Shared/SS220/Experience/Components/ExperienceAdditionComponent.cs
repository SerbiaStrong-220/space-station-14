// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// NOTE:
// This can be ProtoId, but for now it is pointless
public enum ExperienceAddSource : int
{
    RoundStart,
    Job,
    Role,
    Antag
}

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ExperienceAdditionComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables, AlwaysPushInheritance]
    public Dictionary<ExperienceAddSource, ExperienceAddSpecifier> Sources = new();
}

[DataDefinition]
public readonly partial record struct ExperienceAddSpecifier(int FreeSublevelPoints, HashSet<ProtoId<KnowledgePrototype>> Knowledges);
