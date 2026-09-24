using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Objectives;

[Serializable, NetSerializable]
public sealed class ToggleObjectiveStatusEvent(NetEntity target, NetEntity admin, ObjectiveInfo objectiveInfo) : EntityEventArgs
{
    public NetEntity Target = target;
    public NetEntity Admin = admin;
    public ObjectiveInfo ObjectiveInfo = objectiveInfo;
}

[Serializable, NetSerializable]
public sealed class UpdateAntagonistInfoEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;
}
