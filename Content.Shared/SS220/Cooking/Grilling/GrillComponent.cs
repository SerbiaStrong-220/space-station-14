// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This is used for grills
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GrillComponent : Component
{
    /// <summary>
    /// Sound that plays, when food is on the grill
    /// </summary>
    [DataField]
    public SoundSpecifier GrillSound = new SoundPathSpecifier("/Audio/SS220/Effects/grilling.ogg");

    // Grill visuals
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? GrillingSprite;

    // Grill cooking speed modifier
    [DataField]
    public float CookingMultiplier = 0;

    [ViewVariables, AutoNetworkedField]
    public bool IsGrillOn;

    // To keep track of the grilling sound
    [AutoNetworkedField, NonSerialized]
    public EntityUid? GrillingAudioStream;
}
