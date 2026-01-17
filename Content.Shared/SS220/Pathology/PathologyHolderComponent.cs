// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PathologyHolderComponent : Component
{
    [AutoNetworkedField]
    public Dictionary<ProtoId<PathologyPrototype>, PathologyInstanceData> ActivePathologies = new();
}

[Serializable, NetSerializable]
public sealed partial class PathologyInstanceData(TimeSpan startTime, IPathologyContext? context)
{
    [ViewVariables]
    public TimeSpan StartTime = startTime;

    [ViewVariables]
    public int Level = 0;

    [ViewVariables]
    public int StackCount = SharedPathologySystem.OneStack;

    [ViewVariables]
    public List<IPathologyContext?> PathologyContexts = new() { context };
}
