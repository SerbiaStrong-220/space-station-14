using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SetSelector;

[Serializable, NetSerializable]
public sealed class SetSelectorBoundUserInterfaceState(List<ProtoId<SelectableSetPrototype>> sets, List<int> selected, int max, LocId toolName, LocId toolDesc)
    : BoundUserInterfaceState
{
    public readonly List<ProtoId<SelectableSetPrototype>> Sets = sets;
    public readonly List<int> Selected = selected;
    public readonly int MaxSelectedSets = max;
    public readonly LocId ToolName = toolName;
    public readonly LocId ToolDesc = toolDesc;
}

[Serializable, NetSerializable]
public sealed class SetSelectorChangeSetMessage(int setNumber) : BoundUserInterfaceMessage
{
    public readonly int SetNumber = setNumber;
}

[Serializable, NetSerializable]
public sealed class SetSelectorApproveMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum SetSelectorUIKey : byte
{
    Key
}
