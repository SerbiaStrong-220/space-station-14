// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CultYogg.Lamp;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggLampComponent : Component
{
    public bool Activated;

    /// <summary>
    ///     Whether to automatically set item-prefixes when toggling the flashlight.
    /// </summary>
    /// <remarks>
    ///     Flashlights should probably be using explicit unshaded sprite, in-hand and clothing layers, this is
    ///     mostly here for backwards compatibility.
    /// </remarks>
    [DataField]
    public bool AddPrefix = false;

    /// <summary>
    /// Whether or not the light can be toggled via standard interactions
    /// (alt verbs, using in hand, etc)
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    [DataField]
    public EntityUid? ToggleActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleLight";

    [DataField]
    public EntityUid? SelfToggleActionEntity;

    [DataField("turnOnSound")]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/lamp_on.ogg");

    [DataField("turnOffSound")]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/lamp_off.ogg");
}
