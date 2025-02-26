using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MinorFauna.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityCocoonComponent : Component
{
    /// <summary>
    /// ID of the container in which the entities placed in the cocoon are stored
    /// </summary>
    [DataField("container", required: true)]
    public string CocoonContainerId = "cocoon";


}
