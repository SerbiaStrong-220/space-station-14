// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtOnEquipComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ProtoId<MartialArtPrototype> MartialArt;

    /// <summary>
    /// Whether or not this item could override existing user's martial art, user will just lose it
    /// </summary> 
    [DataField]
    [AutoNetworkedField]
    public bool OverrideExisting = false;

    [DataField]
    [AutoNetworkedField]
    public bool Granted = false;
}
