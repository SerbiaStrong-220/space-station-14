// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SuperMatter.Emitter;

[RegisterComponent, NetworkedComponent]
public sealed partial class SuperMatterEmitterBoltComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float MatterEnergyRatio = 0.5f;
    [ViewVariables(VVAccess.ReadOnly)]
    public float PowerConsumedToNormal = 1.2f;
}
