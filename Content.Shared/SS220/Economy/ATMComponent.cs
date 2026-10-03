// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class EconomyATMComponent : Component
{
    [ViewVariables]
    public CardStateEnum CardState = CardStateEnum.Default;

    [ViewVariables]
    public string InfoMessage = string.Empty;

    [ViewVariables]
    public BankAccount BankAccount = new();

    [ViewVariables]
    public string PinInput = string.Empty;

    [ViewVariables]
    public bool UnemployedAlert = false;

    [DataField]
    public SoundSpecifier SoundInsertCurrency = new SoundPathSpecifier("/Audio/SS220/Machines/EconomyATM/polaroid2.ogg");

    [DataField]
    public SoundSpecifier SoundWithdrawCurrency = new SoundPathSpecifier("/Audio/SS220/Machines/EconomyATM/polaroid1.ogg");

    [DataField]
    public SoundSpecifier SoundApply = new SoundPathSpecifier("/Audio/Machines/chime.ogg");

    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [DataField]
    public ProtoId<ToolQualityPrototype> ATMResetMethod = "Screwing";

    [DataField]
    public float ATMResetDelay = 5.0f;
}

[Serializable, NetSerializable]
public enum EconomyATMUiKey
{
    Key
}
