// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This is used for entities that can be cooked on the grill
/// </summary>
[RegisterComponent]
public sealed partial class GrillableComponent : Component
{
    [DataField]
    public float TimeToCook { get; set; } = 120f;

    [DataField]
    public string CookingResult { get; set; }

    [ViewVariables]
    public float CurrentCookTime { get; set; }

    [ViewVariables]
    public SoundSpecifier CookingDoneSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");
}
