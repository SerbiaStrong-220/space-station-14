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
    public readonly NetEntity EntityUid;
    public readonly NetEntity AntagonistEntityUid;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;

    public AntagonistInfoEvent(NetEntity entityUid, NetEntity antagonistEntityUid, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives)
    {
        EntityUid = entityUid;
        AntagonistEntityUid = antagonistEntityUid;
        JobTitle = jobTitle;
        Objectives = objectives;
    }
}
