// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Temperature;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This is used for grills
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrillComponent : Component
{
    /// <summary>
    /// Sound that plays, when food is on the grill
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier GrillSound = new SoundPathSpecifier("/Audio/SS220/Effects/grilling.ogg",
                                            AudioParams.Default.WithVariation(0.1f).WithLoop(true));

    // Grill visuals
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? GrillingSprite;

    [DataField, AutoNetworkedField]
    public float CookingSpeed;

    [ViewVariables, AutoNetworkedField]
    public EntityHeaterSetting GrillSettings = EntityHeaterSetting.Off;

    // To keep track of the grilling sound
    public EntityUid? GrillingAudioStream;
}
