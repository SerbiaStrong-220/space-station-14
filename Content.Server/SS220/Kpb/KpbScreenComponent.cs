using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.SS220.Kpb;

[RegisterComponent]
public sealed partial class KpbScreenComponent : Component
{
    [DataField]
    public DoAfterId? DoAfter;

    /// <summary>
    /// Magic mirror target, used for validating UI messages.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// doafter time required to add a new slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to remove a existing slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to change slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to recolor slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sound emitted when slots are changed
    /// </summary>
    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField("hairAction"), AutoNetworkedField]
    public EntProtoId Action = "ActionKpbScreen";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
