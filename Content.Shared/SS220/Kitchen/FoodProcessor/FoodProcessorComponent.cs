// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Kitchen.FoodProcessor;

/// <summary>
/// A machine that process inserted <see cref="FoodProcessorIngredientComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedFoodProcessorSystem))]
public sealed partial class FoodProcessorComponent : Component
{
    public const string InputContainerId = "foodProcessorInput";

    [DataField]
    public int Capacity = 6;

    [DataField]
    public float ProcessingTime = 3.5f;

    [DataField]
    public SoundSpecifier ProcessingSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    [ViewVariables]
    public Container InputContainer = default!;

    [ViewVariables]
    public float RemainingProcessingTime;

    [ViewVariables]
    public EntityUid? AudioStream;
}
