using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Runes;

[Serializable, NetSerializable]
public sealed partial class CreateNarsiRuneDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId Prototype;

    public CreateNarsiRuneDoAfterEvent(EntProtoId prototype)
    {
        Prototype = prototype;
    }
}
