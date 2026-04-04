using Content.Shared.Chat;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component allows an entity to directly translate spoken text into radio messages (effectively an intrinsic
///     radio headset).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IntrinsicRadioTransmitterComponent : Component
{
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new() { SharedChatSystem.CommonChannel };

    //SS220 PAI with encryption keys begin
    /// <summary>
    ///     Channels that an entity can use by encryption keys
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<RadioChannelPrototype>> EncryptionKeyChannels = new();
    //SS220 PAI with encryption keys end
}
