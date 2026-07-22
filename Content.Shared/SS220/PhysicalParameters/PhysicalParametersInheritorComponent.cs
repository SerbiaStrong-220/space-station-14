// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PhysicalParameters;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhysicalParametersInheritorComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public HashSet<Parameter> ParametersToMove = new HashSet<Parameter>
    {
      { Parameter.ReactionSpeed},
      { Parameter.Coordination}
    };

    [DataField]
    [AutoNetworkedField]
    public bool AddParameters = false; //If false the parameters will be replaced 
}
