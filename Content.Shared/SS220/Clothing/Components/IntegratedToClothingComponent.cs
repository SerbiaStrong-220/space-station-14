// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntegratedToClothingComponent : Component
{
    /// <summary>
    ///     The Uid of the piece of clothing that this entity belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedUid;
}
