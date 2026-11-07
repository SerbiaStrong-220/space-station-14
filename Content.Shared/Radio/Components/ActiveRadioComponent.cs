using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is required to receive radio message events.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveRadioComponent : Component
{
    /// <summary>
    ///     The channels that this radio is listening on.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    // SS220-add-frequency-radio-begin
    /// <summary>
    ///     The channels that this radio is listening on.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> FrequencyChannels = new();
    // SS220-add-frequency-radio-end

    /// <summary>
    /// A toggle for globally receiving all radio channels.
    /// Overrides <see cref="Channels"/>
    /// </summary>
    [DataField]
    public bool ReceiveAllChannels;

    /// <summary>
    ///     If this radio can hear all messages on all maps
    /// </summary>
    [DataField]
    public bool GlobalReceive = false;

    // SS220-listen-only-radio-begin
    /// <summary>
    /// Channels that can only be received, not transmitted to.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> ListenOnlyChannels = new();
    // SS220-listen-only-radio-end
}
