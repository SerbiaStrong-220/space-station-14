// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.AltBlocking;

/// <summary>
/// This component goes on an item that you want to use to block
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class AltBlockingComponent : Component
{
    /// <summary>
    /// The entity that's blocking
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Is it currently blocking?
    /// </summary>
    [DataField]
    public bool IsBlocking;


    /// <summary>
    /// The sound to be played when you get hit while actively blocking
    /// </summary>
    [DataField]
    public SoundSpecifier BlockSound =
        new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg")
        {
            Params = AudioParams.Default.WithVariation(0.25f)
        };

    [DataField]
    public Dictionary<SlotFlags, float> AvaliableSlots = new();

    [DataField, AutoNetworkedField]
    public float RangeBlockProb = 0.5f;

    [DataField, AutoNetworkedField]
    public float ActiveRangeBlockProb = 0.65f;

    [DataField, AutoNetworkedField]
    public float MeleeBlockProb = 0.5f;

    [DataField, AutoNetworkedField]
    public float ActiveMeleeBlockProb = 0.65f;
}
