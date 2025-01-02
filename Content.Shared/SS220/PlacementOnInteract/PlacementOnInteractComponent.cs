using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.PlacementOnInteract;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlacementOnInteractComponent : Component
{
    [ViewVariables]
    public bool IsActive = false;

    [DataField(required: true)]
    public EntProtoId ProtoId;

    [DataField]
    public float DoAfter = 0;
}
