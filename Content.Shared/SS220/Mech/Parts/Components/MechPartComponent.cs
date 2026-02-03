using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="MechComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechPartComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")] public float InstallDuration = 5;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? PartOwner;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool ToBeRemoved = false;

    /// <summary>
    /// The slot this part can be attached to
    /// </summary>
    [DataField("slot")]
    public string slot = "core";

    /// <summary>
    /// A container for storing the equipment entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    //public Container EquipmentContainer = default!;
    public Container EquipmentContainer = new();

    [ViewVariables]
    public readonly string EquipmentContainerId = "part-mech-equipment-container";

    /// <summary>
    /// The maximum amount of equipment items that can be installed in the mech
    /// </summary>
    [DataField("maxEquipmentAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxEquipmentAmount = 3;

    /// <summary>
    /// A whitelist for inserting equipment items.
    /// </summary>
    [DataField]
    public EntityWhitelist? EquipmentWhitelist;

    /// <summary>
    /// How much "health" the mech has left.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Integrity;

    /// <summary>
    /// The maximum amount of damage the mech can take.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxIntegrity = 250;

    /// <summary>
    /// How much does this part weight
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public FixedPoint2 OwnMass = 0;

    /// <summary>
    /// Whether the mech has been destroyed and is no longer pilotable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken = false;

    [DataField]
    [AutoNetworkedField]
    public SpriteSpecifier? AttachedSprite;
}

/// <summary>
/// Raised on the equipment when the installation is finished successfully
/// </summary>
public sealed class MechPartInstallFinished : EntityEventArgs
{
    public EntityUid Mech;

    public MechPartInstallFinished(EntityUid mech)
    {
        Mech = mech;
    }
}

/// <summary>
/// Raised on the equipment when the installation fails.
/// </summary>
public sealed class MechPartInstallCancelled : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class GrabberDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class InsertPartEvent : SimpleDoAfterEvent
{
}

