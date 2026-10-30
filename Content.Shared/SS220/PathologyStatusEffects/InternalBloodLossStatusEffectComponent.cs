// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PathologyStatusEffects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InternalBloodLossStatusEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 1f;

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 BloodLossRatePerStack = 0.55f;

    [AutoNetworkedField]
    public TimeSpan NextEffectTime;
}
