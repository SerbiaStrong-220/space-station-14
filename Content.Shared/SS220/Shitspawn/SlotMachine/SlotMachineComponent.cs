using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Shitspawn.SlotMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlotMachineComponent : Component
{
    public const int MinBet = 100;
    public const string CreditStackId = "Credit";
    public const string CashPrototypeId = "SpaceCash";

    [DataField, AutoNetworkedField]
    public List<string> Reels = new() { "seven", "seven", "seven" };

    [DataField]
    public List<SlotMachineReelDef> ReelPools = new();

    [DataField, AutoNetworkedField]
    public List<SlotMachineRule> Rules = new();

    [DataField, AutoNetworkedField]
    public int StoredCredits;

    [DataField, AutoNetworkedField]
    public int LastBet;

    [DataField, AutoNetworkedField]
    public int LastPayout;

    [AutoNetworkedField]
    public bool IsWin = false;

    [AutoNetworkedField]
    public string WinText = "";

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Machines/id_insert.ogg");

    [DataField]
    public SoundSpecifier SpinSound = new SoundPathSpecifier("/Audio/SS220/Shitspawn/SlotMachine/slot_spin.ogg");

    [DataField]
    public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/SS220/Shitspawn/SlotMachine/slot_win.ogg");

    public bool HasPendingResult;
    public List<string> PendingReels = new() { "seven", "seven", "seven" };
    public bool PendingIsWin = false;
    public string PendingWinText = "";
    public int PendingPayout;
    public TimeSpan SpinEndTime;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SlotMachineRule
{
    [DataField]
    public List<string>? Symbols;

    [DataField]
    public int? Index;

    [DataField]
    public int Multiplier;

    [DataField]
    public string WinText = "";

    [DataField]
    public Color MultiplierColor = Color.White;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SlotMachineSymbolDef
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField]
    public string Name = "slot-machine-symbol-default";

    [DataField]
    public float Weight = 1f;

    [DataField]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SlotMachineReelDef
{
    [DataField(required: true)]
    public List<SlotMachineSymbolDef> Symbols = new();
}
