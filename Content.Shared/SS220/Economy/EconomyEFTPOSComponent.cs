// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class EconomyEFTPOSComponent : Component
{
    [ViewVariables]
    public int OwnerBankAccountId = default;

    [ViewVariables]
    public int Amount = default;

    [ViewVariables]
    public int PayerBankAccountId = default;

    [ViewVariables]
    public string PayerPinInput = string.Empty;

    [ViewVariables]
    public bool PrintReceipt = false;

    [DataField]
    public SoundSpecifier SoundApply = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public ProtoId<ToolQualityPrototype> EFTPOSResetMethod = "Screwing";

    [DataField]
    public float EFTPOSResetDelay = 5.0f;

    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    [DataField]
    public EntProtoId MachineOutput = "TransactionReceiptPaper";
}

[Serializable, NetSerializable]
public enum EconomyEFTPOSKey
{
    Key
}
