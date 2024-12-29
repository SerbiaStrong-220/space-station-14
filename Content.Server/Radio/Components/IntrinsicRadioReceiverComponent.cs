using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;

/// <summary>
///     This component allows an entity to directly translate radio messages into chat messages. Note that this does not
///     automatically add an <see cref="ActiveRadioComponent"/>, which is required to receive radio messages on specific
///     channels.
/// </summary>
[RegisterComponent]
public sealed partial class IntrinsicRadioReceiverComponent : Component
{
    //SS220 PAI with encryption keys begin
    /// <summary>
    ///     Channels that will not be deleted from <see cref="ActiveRadioComponent"/> when extracting the encryption key from the entity
    /// </summary>
    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new();
    //SS220 PAI with encryption keys end
}
