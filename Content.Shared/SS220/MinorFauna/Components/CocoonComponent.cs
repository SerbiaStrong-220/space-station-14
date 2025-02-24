using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MinorFauna.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityCocoonComponent : Component
{
    /// <summary>
    /// ID of the container in which the entities placed in the cocoon are stored
    /// </summary>
    [DataField("container", required: true)]
    public string CocoonContainerId = "cocoon";

    /// <summary>
    /// The entity that created this cocoon
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CocoonOwner;
}
