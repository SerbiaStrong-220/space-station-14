// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.MechClothing;

/// <summary>
/// This handles placing containers in claw when the player uses an action, copies part of the logic MechGrabberComponent
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MechClothingComponent : Component
{
    /// <summary>
    /// The change in energy after each grab.
    /// </summary>
    [DataField]
    public float GrabEnergyDelta = -30;

    /// <summary>
    /// How long does it take to grab something?
    /// </summary>
    [DataField]
    public float GrabDelay = 2.5f;

    /// <summary>
    /// The sound played when a mech is grabbing something
    /// </summary>
    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    public EntityUid? AudioStream;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ItemContainer = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;

    [ViewVariables]
    public EntityUid MechUid;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentEquipmentUid;

    [DataField]
    public string ContainerName = "item-container";
}
