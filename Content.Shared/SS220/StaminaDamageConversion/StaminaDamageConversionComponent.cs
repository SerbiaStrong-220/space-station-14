// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StaminaDamageConversion;


[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaDamageConversionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, float> ConversionDict = new Dictionary<string, float> { { "Shock", 5f }, { "Blunt", 1.2f } };
}
