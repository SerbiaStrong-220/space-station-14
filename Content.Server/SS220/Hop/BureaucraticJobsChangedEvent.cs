// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Hop;

public sealed class BureaucraticJobsChangedEvent(
    EntityUid station, HashSet<ProtoId<JobPrototype>> changedJobs, ProtoId<JobPrototype>? preferredJob) : EntityEventArgs
{
    public readonly EntityUid Station = station;
    public readonly HashSet<ProtoId<JobPrototype>> ChangedJobs = changedJobs;
    public readonly ProtoId<JobPrototype>? PreferredJob = preferredJob;
}
