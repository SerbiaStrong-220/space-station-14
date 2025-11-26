// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtOnTriggerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ProtoId<MartialArtPrototype> MartialArt;
}
