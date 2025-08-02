// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Physics;
using Content.Shared.SS220.Forcefield.Figures;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Forcefield.Components;

/// <summary>
/// A component that creates a force field that blocks or allows entities to pass through.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldComponent : Component
{
    /// <summary>
    /// Force field parameters
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ForcefieldParams Params = new();

    /// <summary>
    /// The entity that created the force field
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? FieldOwner;

    /// <summary>
    /// The sound played when force field damaged
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_hit.ogg", new()
    {
        Volume = 1.25f
    });
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldParams()
{
    [DataField(required: true)]
    public IForcefieldFigure Figure = new ForcefieldParabola();

    [DataField]
    public ForcefieldCollisionOption CollisionOption = ForcefieldCollisionOption.OutsideGoing;

    [DataField]
    public float Density = 1;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>))]
    public int CollisionLayer = (int)CollisionGroup.None;

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>))]
    public int CollisionMask = (int)CollisionGroup.None;

    [DataField]
    public Color Color = Color.LightBlue;

    [DataField]
    public float Visibility = 0.1f;
}

public enum ForcefieldCollisionOption
{
    None = 0,

    InsideGoing = 1 << 0,
    OutsideGoing = 1 << 1,

    All = ~0
}
