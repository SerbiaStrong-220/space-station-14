// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.InternalComponentDamageRelay;

[RegisterComponent]

public sealed partial class InternalComponentDamageRelayComponent : Component
{
    [DataField]
    public ProtoId<WeightedRandomPrototype> Containers = string.Empty;

    [DataField]
    public bool ApplyNegative = false;
}
