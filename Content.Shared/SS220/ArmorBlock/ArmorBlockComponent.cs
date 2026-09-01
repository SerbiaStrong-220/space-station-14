// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ArmorBlock;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class ArmorBlockComponent : Component
{
    /// <summary>
    /// The entity this armor protects(must be set manually in every implementation, made for reusability)
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User = null;
}
