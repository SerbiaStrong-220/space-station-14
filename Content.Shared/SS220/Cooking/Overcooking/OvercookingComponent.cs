// SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Cooking.Overcooking;

/// <summary>
/// Tracks how long cooked food has been overcooking.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class OvercookingComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TimeToOvercook = 15f;

    // Minimum time, at which the entity is considered "Overcooked", so it won't be 0.1s after cooking is done
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MinOvercookingTime = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId OvercookedEntity = "FoodBadRecipe";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentOvercookTime;

    [DataField]
    public SoundSpecifier OvercookedSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");
}
