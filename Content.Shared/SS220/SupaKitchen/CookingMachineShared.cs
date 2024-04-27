using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen;

[Serializable, NetSerializable]
public sealed class CookingMachineStartCookMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CookingMachineEjectMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CookingMachineEjectSolidIndexedMessage : BoundUserInterfaceMessage
{
    public NetEntity EntityID;
    public CookingMachineEjectSolidIndexedMessage(NetEntity entityId)
    {
        EntityID = entityId;
    }
}

[Serializable, NetSerializable]
public sealed class CookingMachineVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
{
    public ReagentQuantity ReagentQuantity;
    public CookingMachineVaporizeReagentIndexedMessage(ReagentQuantity reagentQuantity)
    {
        ReagentQuantity = reagentQuantity;
    }
}

[Serializable, NetSerializable]
public sealed class CookingMachineSelectCookTimeMessage : BoundUserInterfaceMessage
{
    public int ButtonIndex;
    public uint NewCookTime;
    public CookingMachineSelectCookTimeMessage(int buttonIndex, uint inputTime)
    {
        ButtonIndex = buttonIndex;
        NewCookTime = inputTime;
    }
}

[NetSerializable, Serializable]
public sealed class CookingMachineUpdateUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity[] ContainedSolids;
    public bool IsMachineBusy;
    public int ActiveButtonIndex;
    public uint CurrentCookTime;
    public bool EjectUnavailable;

    public CookingMachineUpdateUserInterfaceState(NetEntity[] containedSolids,
        bool isMachineBusy, int activeButtonIndex, uint currentCookTime, bool ejectUnavailable)
    {
        ContainedSolids = containedSolids;
        IsMachineBusy = isMachineBusy;
        ActiveButtonIndex = activeButtonIndex;
        CurrentCookTime = currentCookTime;
        EjectUnavailable = ejectUnavailable;
    }

}

[Serializable, NetSerializable]
public enum CookingMachineVisualState
{
    Idle,
    Cooking,
    Broken
}

[Serializable, NetSerializable]
public enum CookingMachineUiKey
{
    Key
}
