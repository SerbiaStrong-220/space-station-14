using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.SS220.Arena;

[RegisterComponent]
public sealed partial class ArenaParticipantComponent : Component
{
    [DataField]
    public ArenaSlot Slot;
}
