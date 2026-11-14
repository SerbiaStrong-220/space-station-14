// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.Hop;

/// <summary>Temporary event overrides. Totals include occupied slots so hires during the event are accounted for.</summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HopStationRestrictionsComponent : Component
{
    [DataField]
    public HashSet<ProtoId<JobPrototype>> LockedJobs = new();

    [DataField]
    public ProtoId<JobPrototype>? ForcedPriority;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int?> SavedTotals = new();

    [DataField]
    public HashSet<ProtoId<JobPrototype>> SavedPriorities = new();

    [DataField]
    public HashSet<EntityUid> Rules = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? RestoreAt;
}
