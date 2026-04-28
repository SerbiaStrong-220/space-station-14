// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StaminaDamageConversion;


[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaDamageConversionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, float> ConversionDict = new Dictionary<string, float> { { "Shock", 5f }, { "Blunt", 1f } };
}
