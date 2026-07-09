// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;


[RegisterComponent]
public sealed partial class VirusHolderComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Viruses = [];
}
