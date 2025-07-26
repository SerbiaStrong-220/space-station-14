
using Content.Shared.SS220.Forcefield.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Forcefield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public IForcefieldFigure Figure = default;

    [DataField, AutoNetworkedField]
    public float Destiny;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>)), AutoNetworkedField]
    public int CollisionLayer;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>)), AutoNetworkedField]
    public int CollisionMask;
}
