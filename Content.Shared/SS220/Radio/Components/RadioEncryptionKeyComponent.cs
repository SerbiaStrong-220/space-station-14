// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Radio.Components;

/// <summary>
/// Handles handheld radio ui and is authoritative on the channels a radio can access.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RadioEncryptionKeyComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 RadioFrequency;

    /// <summary>
    /// Lower border for radio channel frequency
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 LowerFrequencyBorder;

    /// <summary>
    /// Lower border for radio channel frequency
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 UpperFrequencyBorder;
}
