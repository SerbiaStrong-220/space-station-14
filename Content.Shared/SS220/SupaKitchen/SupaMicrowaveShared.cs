using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen;

[Serializable, NetSerializable]
public sealed class SupaMicrowaveStartCookMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class SupaMicrowaveEjectMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class SupaMicrowaveEjectSolidIndexedMessage : BoundUserInterfaceMessage
{
    public NetEntity EntityID;
    public SupaMicrowaveEjectSolidIndexedMessage(NetEntity entityId)
    {
        EntityID = entityId;
    }
}

[Serializable, NetSerializable]
public sealed class SupaMicrowaveVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
{
    public ReagentQuantity ReagentQuantity;
    public SupaMicrowaveVaporizeReagentIndexedMessage(ReagentQuantity reagentQuantity)
    {
        ReagentQuantity = reagentQuantity;
    }
}

[Serializable, NetSerializable]
public sealed class SupaMicrowaveSelectCookTimeMessage : BoundUserInterfaceMessage
{
    public int ButtonIndex;
    public uint NewCookTime;
    public SupaMicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime)
    {
        ButtonIndex = buttonIndex;
        NewCookTime = inputTime;
    }
}

[NetSerializable, Serializable]
public sealed class SupaMicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity[] ContainedSolids;
    public SupaMicrowaveState State;
    public int ActiveButtonIndex;
    public uint CurrentCookTime;
    public bool EjectUnavailable;

    public SupaMicrowaveUpdateUserInterfaceState(NetEntity[] containedSolids,
        SupaMicrowaveState state, int activeButtonIndex, uint currentCookTime, bool ejectUnavailable)
    {
        ContainedSolids = containedSolids;
        State = state;
        ActiveButtonIndex = activeButtonIndex;
        CurrentCookTime = currentCookTime;
        EjectUnavailable = ejectUnavailable;
    }
}

[Serializable, NetSerializable]
public enum SupaMicrowaveState
{
    Idle,
    UnPowered,
    Cooking,
    Broken
}

[Serializable, NetSerializable]
public enum SupaMicrowaveVisualState
{
    Idle,
    Cooking,
    Broken
}

[Serializable, NetSerializable]
public enum SupaMicrowaveUiKey
{
    Key
}
