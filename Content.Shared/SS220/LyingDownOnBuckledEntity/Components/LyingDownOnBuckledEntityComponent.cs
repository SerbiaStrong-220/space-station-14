// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.SS220.LyingDownOnBuckledEntity.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LyingDownOnBuckledEntityComponent : Component
{
    /// <summary>
    /// Is this pet lying on entity
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsLying = false;

    /// <summary>
    /// Uid of the stand up action
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ActionUid;

    /// <summary>
    /// Whether damage will cause StandUp of lyed entity
    /// </summary>
    [DataField]
    public FixedPoint2 DamagetThreshold = 1;

    /// <summary>
    /// Uid of the entity on which the pet is lying
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LyingOn;

    /// <summary>
    /// What entity do you need to buckle to take damage
    /// </summary>
    [DataField]
    public EntityWhitelist? StrapWhitelist;

    /// <summary>
    /// Offset of lying pet regarding the entity on which lies
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Should pet blocks unbuckle of the entity on which lies
    /// </summary>
    [DataField]
    public bool BlockUnbuckle = false;

    /// <summary>
    /// Damage to entity on which pet lies.
    /// If null - doesn't cause damage
    /// </summary>
    [DataField]
    public DamageOnLying? DamageOnLying;

    [DataField]
    public TimeSpan LyingDoAfter = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan RaiseDoAfter = TimeSpan.FromSeconds(3);
}

[DataDefinition]
public sealed partial class DamageOnLying
{
    /// <summary>
    /// The damage that the entity gain per second
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entities that damaged by this entity
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
