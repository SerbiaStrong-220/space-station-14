// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// Revive state of a person carried by their extracted brain, so a brain transplant doesn't reset deaths and brain damage.
/// The brain rot timer is frozen while the brain is outside a body.
/// </summary>
[RegisterComponent]
public sealed partial class BrainReviveStateComponent : Component
{
    [ViewVariables]
    public int DeathCounter;

    [ViewVariables]
    public TimeSpan? DamageCountingTime;

    [ViewVariables]
    public FixedPoint2 CerebralDamage;
}
