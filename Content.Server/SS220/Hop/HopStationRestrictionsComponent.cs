// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Hop;

/// <summary>
/// Bureaucratic changes last for the shift, just like the event's changes to job slots.
/// All affected jobs are locked; only one can be the station's forced late-join priority.
/// </summary>
[RegisterComponent]
public sealed partial class HopStationRestrictionsComponent : Component
{
    [DataField]
    public HashSet<ProtoId<JobPrototype>> LockedJobs = new();

    [DataField]
    public ProtoId<JobPrototype>? ForcedPriority;
}
