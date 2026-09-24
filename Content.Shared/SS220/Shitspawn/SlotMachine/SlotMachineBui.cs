using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shitspawn.SlotMachine;

[Serializable, NetSerializable]
public enum SlotMachineUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlotMachineBoundUserInterfaceState : BoundUserInterfaceState
{
    public string[] Reels { get; }
    public int StoredCredits { get; }
    public SlotMachineResult LastResult { get; }
    public int LastBet { get; }
    public int LastPayout { get; }
    public bool IsSpinning { get; }

    public SlotMachineBoundUserInterfaceState(string[] reels, int storedCredits, SlotMachineResult lastResult, int lastBet, int lastPayout, bool isSpinning)
    {
        Reels = reels;
        StoredCredits = storedCredits;
        LastResult = lastResult;
        LastBet = lastBet;
        LastPayout = lastPayout;
        IsSpinning = isSpinning;
    }
}

[Serializable, NetSerializable]
public sealed class SlotMachineSpinMessage : BoundUserInterfaceMessage
{
    public int Bet { get; }
    public SlotMachineSpinMessage(int bet) => Bet = bet;
}

[Serializable, NetSerializable]
public sealed class SlotMachineInsertMessage : BoundUserInterfaceMessage
{
    public int Amount { get; }
    public SlotMachineInsertMessage(int amount) => Amount = amount;
}

[Serializable, NetSerializable]
public sealed class SlotMachineCollectMessage : BoundUserInterfaceMessage
{
}
