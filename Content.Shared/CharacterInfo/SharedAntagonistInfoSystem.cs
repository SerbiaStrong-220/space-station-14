using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestAntagonistInfoEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public RequestAntagonistInfoEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}

[Serializable, NetSerializable]
public sealed class AntagonistInfoEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ConditionInfo>> Objectives;
    public readonly string Briefing;

    public AntagonistInfoEvent(EntityUid entityUid, string jobTitle, Dictionary<string, List<ConditionInfo>> objectives, string briefing)
    {
        EntityUid = entityUid;
        JobTitle = jobTitle;
        Objectives = objectives;
        Briefing = briefing;
    }
}
