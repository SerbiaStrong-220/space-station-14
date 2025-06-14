// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Undereducated;

[RegisterComponent]
public sealed partial class UndereducatedComponent : Component
{
    [DataField]
    public string Language = "";

    [DataField]
    public float ChanseToReplace = 0.05f;
}
