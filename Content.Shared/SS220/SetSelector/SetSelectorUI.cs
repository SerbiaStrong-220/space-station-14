using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SetSelector;

[Serializable, NetSerializable]
public sealed class SetSelectorBoundUserInterfaceState(Dictionary<int, SelectableSetInfo> sets, int max, LocId toolName, LocId toolDesc)
    : BoundUserInterfaceState
{
    public readonly Dictionary<int, SelectableSetInfo> Sets = sets;
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
};

[Serializable, NetSerializable, DataDefinition]
public partial struct SelectableSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public SelectableSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
