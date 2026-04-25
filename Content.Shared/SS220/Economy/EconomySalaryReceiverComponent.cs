// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Economy;

/// <summary>
/// Entity with this component 'owns' a bank account.
/// </summary>
/// <remarks>
/// Clients shouldn't get fields with data, only default onces.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class EconomySalaryReceiverComponent : Component
{
    [DataField]
    public int AccountId = default;

    [DataField]
    public int AccountPin = default;
}
