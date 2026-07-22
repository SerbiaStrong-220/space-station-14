// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PhysicalParameters;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhysicalParametersModifyingClothingComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<Parameter, FixedPoint2> ParameterDict = new Dictionary<Parameter, FixedPoint2>
    {
      { Parameter.Strength, 1}
    };

    [DataField]
    public bool DependsOnActivation = true;
}
