// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Arena;

[RegisterComponent]
public sealed partial class ArenaParticipantComponent : Component
{
    [DataField]
    public ArenaSlot Slot;
}
