// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Arena;

[RegisterComponent]
public sealed partial class ArenaParticipantComponent : Component
{
    [DataField(required: true)]
    public string Team = string.Empty;

    [DataField]
    public ProtoId<FactionIconPrototype>? Icon;

    [ViewVariables]
    public bool Equipped;
}
