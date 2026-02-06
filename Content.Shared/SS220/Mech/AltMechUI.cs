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

[Serializable, NetSerializable]
public sealed class MechMaintenanceToggleMessage : BoundUserInterfaceMessage
{
    public bool Toggled;

    public MechMaintenanceToggleMessage(bool toggled)
    {
        Toggled = toggled;
    }
}

[Serializable, NetSerializable]
public sealed class AltMechBoundUiState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, BoundUserInterfaceState> EquipmentStates = new();

    //public Dictionary<string, NetEntity?> Parts = new();
}
