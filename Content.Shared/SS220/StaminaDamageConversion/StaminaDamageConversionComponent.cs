using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StaminaDamageConversion;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaDamageConversionComponent : Component
{
    public Dictionary<string, float> ConversionDict = new Dictionary<string, float> { { "Shock", 5f }, { "Blunt", 1f } };
}
