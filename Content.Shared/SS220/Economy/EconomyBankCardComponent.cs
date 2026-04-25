// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Economy;

/// <summary>
/// Entity with this component could be used for bank payments.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EconomyBankCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public int AccountId = default;
}
