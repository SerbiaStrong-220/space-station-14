// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Wieldable;

[RegisterComponent, NetworkedComponent]
public sealed partial class StrongMinerComponent : Component
{

    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> Species = [];
}

