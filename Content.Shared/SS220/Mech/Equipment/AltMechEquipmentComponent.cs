// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mech.Components;

namespace Content.Shared.SS220.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="MechComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class AltMechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")] public float InstallDuration = 5;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables] public EntityUid? EquipmentOwner;
}

/// <summary>
/// Raised on the equipment when the installation is finished successfully
/// </summary>
public sealed class AltMechEquipmentInstallFinished : EntityEventArgs
{
    public EntityUid Mech;

    public AltMechEquipmentInstallFinished(EntityUid mech)
    {
        Mech = mech;
    }
}

/// <summary>
/// Raised on the equipment when the installation fails.
/// </summary>
public sealed class AltMechEquipmentInstallCancelled : EntityEventArgs
{
}

