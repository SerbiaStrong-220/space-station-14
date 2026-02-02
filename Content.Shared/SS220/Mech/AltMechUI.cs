using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AltMech;

/// <summary>
/// UI event raised to remove a part from a mech
/// </summary>
[Serializable, NetSerializable]
public sealed class MechPartRemoveMessage : BoundUserInterfaceMessage
{
    public string Part;

    public MechPartRemoveMessage(string part)
    {
        Part = part;
    }
}
