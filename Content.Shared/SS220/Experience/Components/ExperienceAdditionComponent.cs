// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// NOTE:
// This can be ProtoId, but for now it is pointless
public enum ExperienceAddSource : int
{
    Admin = 0,
    RoundStart,
    Job,
    Role,
    Antag
}

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ExperienceAdditionComponent : Component
{
    [AlwaysPushInheritance]
    [DataField, AutoNetworkedField]
    public Dictionary<ExperienceAddSource, ExperienceAddSpecifier> Sources = new();

    [AlwaysPushInheritance]
    [DataField, AutoNetworkedField]
    public ProtoId<ExperienceDefinitionPrototype>? DefinitionId;

    [DataField, AutoNetworkedField]
    public bool Forced;

    // NOTE: If you need to more 'flags' like Forced
    // Its better to actually make this changeable field
    public static readonly HashSet<ExperienceAddSource> AdminForceWhitelistSources = [ExperienceAddSource.Admin, ExperienceAddSource.Antag];
}

[DataDefinition]
public readonly partial record struct ExperienceAddSpecifier(int FreeSublevelPoints, HashSet<ProtoId<KnowledgePrototype>> Knowledges, Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo> Skills);
