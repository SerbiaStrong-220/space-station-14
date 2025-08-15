// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.TouretteSyndrome;

[RegisterComponent]
[Access(typeof(TouretteAccentSystem))]
public sealed partial class TouretteAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public float SwearChance;
    public List<string> TouretteWords;
}
