// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PathologyHolderComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<ProtoId<PathologyPrototype>, PathologyInstanceData> ActivePathologies = new();

    /// <summary>
    /// When host died, while it stays dead. On revival every stage timer is shifted forward by the
    /// time spent here, so the dormant pathology resumes exactly where it froze.
    /// </summary>
    [ViewVariables]
    public TimeSpan? DiedAt;
}

[Serializable, NetSerializable]
public sealed partial class PathologyInstanceData(TimeSpan startTime, IPathologyContext? context)
{
    [ViewVariables]
    public TimeSpan StartTime = startTime;

    /// <summary>When current stage began. Reset on every progression so a stage's end.</summary>
    [ViewVariables]
    public TimeSpan StageStartTime = startTime;

    [ViewVariables]
    public int Level = 0;

    [ViewVariables]
    public int StackCount = SharedPathologySystem.OneStack;

    [ViewVariables]
    public List<IPathologyContext?> PathologyContexts = new() { context };

    /// <summary>
    /// Component names this pathology actually added to the host — only ones it didn't already
    /// carry. Cure strips just these, so a component the host owned beforehand (e.g. from a trait
    /// that shares it with a symptom) survives.
    /// </summary>
    [ViewVariables]
    public HashSet<string> AddedComponents = new();
}
