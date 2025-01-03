// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PlacerItem.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PlacerItemComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Active = false;

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

    [DataField(required: true)]
    public EntProtoId ProtoId;

    [DataField]
    public float DoAfter = 0;

    [DataField]
    public bool ToggleActiveOnUseInHand = false;
}
