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
    public readonly EntityUid AntagonistEntityUid;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ConditionInfo>> Objectives;

    public AntagonistInfoEvent(EntityUid entityUid, EntityUid antagonistEntityUid, string jobTitle, Dictionary<string, List<ConditionInfo>> objectives)
    {
        EntityUid = entityUid;
        AntagonistEntityUid = antagonistEntityUid;
        JobTitle = jobTitle;
        Objectives = objectives;
    }
}
