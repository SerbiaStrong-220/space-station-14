// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This is used for entities that can be cooked on the grill
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrillableComponent : Component
{
    [DataField]
    public float TimeToCook = 120f;

    [DataField]
    public string CookingResult;

    [DataField]
    public float AlmostDoneCookPercentage = 0.75f;

    [DataField]
    // 473f - ideal grill temp in Kelvins
    public float IdealGrillingTemperature = (200 + Atmospherics.T0C);

    [DataField]
    public SoundSpecifier CookingDoneSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");

    [ViewVariables, AutoNetworkedField]
    public float CurrentCookTime;

    [ViewVariables, AutoNetworkedField]
    public bool IsCooking;
}
