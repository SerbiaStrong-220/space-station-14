// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Undereducated;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UndereducatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Language = "";

    [DataField, AutoNetworkedField]
    public float ChanseToReplace = 0.05f;

    [DataField, AutoNetworkedField]
    public bool Tuned = false;
}
