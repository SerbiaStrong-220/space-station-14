using Content.Shared.DoAfter;
using Content.Shared.Mech.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="MechComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class MechPartComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")] public float InstallDuration = 5;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables] public EntityUid? PartOwner;

    /// <summary>
    /// The slot this part can be attached to
    /// </summary>
    [DataField("slot")] public string slot;
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

