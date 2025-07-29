// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Forcefield.Figures;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Forcefield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public IForcefieldFigure Figure = default;

    [DataField, AutoNetworkedField]
    public ForcefieldCollisionOption CollisionOption = ForcefieldCollisionOption.OutsideGoing;

    [DataField, AutoNetworkedField]
    public float Density;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>)), AutoNetworkedField]
    public int CollisionLayer;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>)), AutoNetworkedField]
    public int CollisionMask;

    [DataField, AutoNetworkedField]
    public Color Color = Color.LightBlue;

    [DataField, AutoNetworkedField]
    public float Visibility = 0.1f;

    [DataField, AutoNetworkedField]
    public NetEntity? FieldOwner;

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_hit.ogg", new()
    {
        Volume = 1.25f
    });
}

public enum ForcefieldCollisionOption
{
    None = 0,

    InsideGoing = 1 << 0,
    OutsideGoing = 1 << 1,

    All = ~0
}
