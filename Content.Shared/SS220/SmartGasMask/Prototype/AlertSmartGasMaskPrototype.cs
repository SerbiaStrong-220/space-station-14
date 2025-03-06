// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    public LocId Name { get; set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/Actions/alert_smart_gas_mask/securitymask.png"));

    //To understand what exactly the player chose
    [DataField(required: true)]
    public NotificationType NotificationType;

    //Sound that will be played when selecting an action
    [DataField, ViewVariables]
    public SoundSpecifier AlertSound { get; set; }  = new SoundPathSpecifier("/Audio/SS220/Items/SmartGasMask/sound_voice_complionator_halt.ogg");

    //Message that will be played when selecting an action
    [DataField]
    public List<LocId> LocIdMessage = new List<LocId>() { };

    [DataField]
    public TimeSpan CoolDown { get; set; } = TimeSpan.FromSeconds(10);

}

public enum NotificationType : byte
{
    Halt,
    Support,
}
