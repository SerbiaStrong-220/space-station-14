// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Pen;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PenComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BrushWriteSize = 1;

    [DataField, AutoNetworkedField]
    public int BrushEraseSize = 2;
}
