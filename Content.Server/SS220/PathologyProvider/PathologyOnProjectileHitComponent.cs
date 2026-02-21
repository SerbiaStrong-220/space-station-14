// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.PathologyProvider;

[RegisterComponent]
public sealed partial class PathologyOnProjectileHitComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float ChanceToApply = 0.6f;

    [DataField]
    [AutoNetworkedField]
    public ProtoId<WeightedRandomPrototype> WeightedRandomPathology = "BulletHitPathology";

    [DataField]
    [AutoNetworkedField]
    public ProtoId<WeightedRandomEntityPrototype>? WeighedRandomProviderEntity = null;
}
