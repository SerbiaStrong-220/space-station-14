// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PlacerItem.Components;

/// <summary>
/// A component that allows the user to place a specific entity using a construction ghost
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PlacerItemComponent : Component
{
    /// <summary>
    /// Is placement by this component active or not
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// In which direction should the construction be placed
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Direction ConstructionDirection
    {
        get => _constructionDirection;
        set
        {
            _constructionDirection = value;
            ConstructionTransform = new Transform(new(), _constructionDirection.ToAngle());
        }
    }

    private Direction _constructionDirection = Direction.South;

    [ViewVariables(VVAccess.ReadOnly)]
    public Transform ConstructionTransform { get; private set; } = default!;

    /// <summary>
    /// Id of the prototype used in the construction ghost
    /// </summary>
    [DataField]
    public EntProtoId? ConstructionGhostProto;

    /// <summary>
    /// Id of the prototype that will be spawned
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnProto;

    /// <summary>
    /// The time required for the user to place the construction
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.Zero;

    /// <summary>
    /// Should the placement toggle when item used in hand
    /// </summary>
    [DataField]
    public bool ToggleActiveOnUseInHand = false;
}
