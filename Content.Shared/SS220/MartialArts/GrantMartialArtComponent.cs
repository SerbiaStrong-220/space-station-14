using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrantMartialArtComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ProtoId<MartialArtPrototype> MartialArt;

    [DataField]
    [AutoNetworkedField]
    public bool DestroyAfterUse = true;
}
