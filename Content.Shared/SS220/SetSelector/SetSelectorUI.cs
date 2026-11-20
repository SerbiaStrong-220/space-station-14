using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SetSelector;

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
