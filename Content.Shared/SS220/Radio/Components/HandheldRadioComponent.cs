// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Radio.Components;

/// <summary>
/// Handles handheld radio ui and is authoritative on the channels a radio can access.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HandheldRadioComponent : Component
{
    /// <summary>
    /// Does this radio require power to function
    /// </summary>
    [DataField]
    public bool RequiresPower;

    /// <summary>
    /// The list of radio channel prototypes this radio can choose between.
    /// </summary>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> SupportedChannels = new();

    /// <summary>
    /// Bordering radio channel
    /// </summary>
    [DataField("lowerBorder")]
    public int LowerFrequencyBorder = 1390;

    [DataField("upperBorder")]
    public int UpperFrequencyBorder = 1399;
}
