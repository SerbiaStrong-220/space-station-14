// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CultYogg.CultYoggLamp;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggLampComponent : Component
{
    public bool Activated;

    [DataField]
    public EntityUid? ToggleActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleLight";

    [DataField]
    public EntityUid? SelfToggleActionEntity;

    [DataField("turnOnSound")]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    [DataField("turnOffSound")]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");
}
