// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PhysicalParameters;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhysicalParametersModifyingClothingComponent : Component // This component represents a set of parameters common for most playable beings to determine their physiological capabilities so they can be used in other systems.
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<Parameter, FixedPoint2> ParameterDict = new Dictionary<Parameter, FixedPoint2>
    {
      { Parameter.Strength, 1},
      { Parameter.ReactionSpeed, 1},
      { Parameter.Coordination, 1},
      { Parameter.PainTolerance, 1}
    };

    [DataField]
    [AutoNetworkedField]
    public bool AddParameters = true; //If false the parameters will be replaced 
}
