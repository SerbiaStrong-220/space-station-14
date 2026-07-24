// SS220 changeling Apex tracker
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Mutations;

/// <summary>
/// Owner-only presentation state for Apex Predator tracking.
/// The selected target is deliberately server-only so nearby clients cannot discover it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ChangelingApexTrackerComponent : Component
{
    /// <summary>
    /// Selected prey. This field must never be networked.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// Current BUI selection tokens mapped to server-only targets. Sending an entity identifier in the
    /// BUI state would leak a stable network identity for crew outside the changeling's PVS.
    /// </summary>
    [ViewVariables]
    public Dictionary<uint, EntityUid> TargetSelectionTokens = [];

    /// <summary>
    /// Monotonic source for short-lived selection tokens. The mapping is rebuilt every time the UI is refreshed,
    /// so stale client messages cannot select a previous target.
    /// </summary>
    [ViewVariables]
    public uint NextSelectionToken = 1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public Angle ArrowAngle;

    public override bool SendOnlyToOwner => true;
}
