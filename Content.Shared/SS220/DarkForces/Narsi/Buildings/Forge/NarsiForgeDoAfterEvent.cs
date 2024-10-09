using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cult;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class NarsiForgeDoAfterEvent : DoAfterEvent
{
    [DataField("entityToSpawn", required: true)]
    public string EntityToSpawn = default!;

    [DataField("sourceEntityUid", required: true)]
    public NetEntity SourceEntityUid = default!;
    public NarsiForgeDoAfterEvent(string entityToSpawn, NetEntity sourceEntityUid)
    {
        EntityToSpawn = entityToSpawn;
        SourceEntityUid = sourceEntityUid;
    }
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
