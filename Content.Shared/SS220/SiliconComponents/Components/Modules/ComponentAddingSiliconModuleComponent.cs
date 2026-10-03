// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
public sealed partial class ComponentAddingSiliconModuleComponent : Component //This will remove all components it adds on removal/destruction, do not use to modify existing components
{
    [DataField]
    public ComponentRegistry Components = new();
}
