// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Trackers.Components;

[RegisterComponent]
public sealed partial class DamageReceivedTrackerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid WhomDamageTrack;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CurrentAmount = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public DamageTrackerSpecifier DamageTracker;

    public float GetProgress()
    {
        if (CurrentAmount > DamageTracker.TargetAmount)
            return 1f;

        return (float)CurrentAmount.Value / DamageTracker.TargetAmount.Value;
    }

}

[DataDefinition]
public struct DamageTrackerSpecifier(string damageGroup, FixedPoint2 amount)
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageGroupPrototype> DamageGroup = damageGroup;

    /// <summary>
    /// if null will count damage in all owner's mob state.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<MobState>? AllowedState;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TargetAmount = amount;
}
