// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.PoweredClothing;

[RegisterComponent]
public sealed partial class ComponentRequiringPoweredClothingComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[]
        {
            "NeuralInterface"
        }
    };
}
