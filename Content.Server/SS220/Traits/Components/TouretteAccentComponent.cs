// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Traits.Systems;

namespace Content.Server.SS220.Traits.Components;

[RegisterComponent]
[Access(typeof(TouretteAccentSystem))]
public sealed partial class TouretteAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public float SwearChance;
    public List<string> TouretteWords;
}
