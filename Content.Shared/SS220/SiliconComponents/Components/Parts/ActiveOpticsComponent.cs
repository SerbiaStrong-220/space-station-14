// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class ActiveOpticsComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public int EyeDamage = 0;
}
