// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SmartGasMask.Prototype;

/// <summary>
/// For selectable actions prototypes in SmartGasMask.
/// </summary>
[Prototype]
public sealed partial class AlertSmartGasMaskPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; set; } = default!;

    [DataField(required: true)]
    public EntProtoId IconPrototype;

    //To understand what exactly the player chose
    [DataField(required: true)]
    public NotificationType NotificationType;

    //Sound that will be played when selecting an action
    [DataField, ViewVariables]
    public SoundSpecifier AlertSound { get; set; }  = new SoundPathSpecifier("/Audio/SS220/Items/SmartGasMask/sound_voice_complionator_halt.ogg");

    //Message that will be played when selecting an action
    [DataField]
    public List<LocId> LocIdMessage = new List<LocId>() { };
}

public enum NotificationType : byte
{
    Halt,
    Support,
}
