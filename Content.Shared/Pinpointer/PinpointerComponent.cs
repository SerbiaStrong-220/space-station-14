using Content.Shared.SS220.Pinpointer;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the target component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerComponent : Component
{
    // TODO: Type serializer oh god
    [DataField("component"), ViewVariables(VVAccess.ReadWrite)]
    public string? Component;

    [DataField("mediumDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float MediumDistance = 16f;

    [DataField("closeDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float CloseDistance = 8f;

    [DataField("reachedDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float ReachedDistance = 1f;

    /// <summary>
    ///     Pinpointer arrow precision in radians.
    /// </summary>
    [DataField("precision"), ViewVariables(VVAccess.ReadWrite)]
    public double Precision = 0.09;

    /// <summary>
    ///     Name to display of the target being tracked.
    /// </summary>
    [DataField("targetName"), ViewVariables(VVAccess.ReadWrite)]
    public string? TargetName;

    /// <summary>
    ///     Whether or not the target name should be updated when the target is updated.
    /// </summary>
    [DataField("updateTargetName"), ViewVariables(VVAccess.ReadWrite)]
    public bool UpdateTargetName;

    /// <summary>
    ///     Whether or not the target can be reassigned.
    /// </summary>
    [DataField("canRetarget"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanRetarget;

    [ViewVariables]
    public EntityUid? Target = null;

    [ViewVariables, AutoNetworkedField]
    public bool IsActive = false;

    [ViewVariables, AutoNetworkedField]
    public Angle ArrowAngle;

    [ViewVariables, AutoNetworkedField]
    public Distance DistanceToTarget = Distance.Unknown;

    [ViewVariables]
    public bool HasTarget => DistanceToTarget != Distance.Unknown;

    //ss220 add pinpointer ui start
    [DataField]
    [AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public HashSet<TrackedItem> Sensors = [];//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public HashSet<TrackedItem> TrackedItems = [];//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public HashSet<TrackedItem> Targets = [];//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public PinpointerMode Mode = PinpointerMode.Crew;//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public EntityUid? TrackedByDnaEntity;//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public string? DnaToTrack;//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219

    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2f);

    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The target component that will be searched for
    /// Not the same variable as "Component" because it is used by different systems
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public string? TargetsComponent;//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    //ss220 add pinpointer ui end
}

[Serializable, NetSerializable]
public enum Distance : byte
{
    Unknown,
    Reached,
    Close,
    Medium,
    Far
}
