using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;

/// <summary>
///     This component allows an entity to directly translate spoken text into radio messages (effectively an intrinsic
///     radio headset).
/// </summary>
[RegisterComponent]
public sealed partial class IntrinsicRadioTransmitterComponent : Component
{
    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new() { SharedChatSystem.CommonChannel };

    //SS220 PAI with encryption keys begin
    /// <summary>
    ///     Channels that an entity can use by encryption keys
    /// </summary>
    [ViewVariables]
    public HashSet<string> EncryptionKeyChannels = new();
    //SS220 PAI with encryption keys end
}
