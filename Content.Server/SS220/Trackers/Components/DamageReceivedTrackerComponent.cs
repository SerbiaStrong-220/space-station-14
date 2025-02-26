// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.SS220.Trackers.Components;

[RegisterComponent]
public sealed partial class DamageReceivedTrackerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid WhomDamageTrack;

    // We use this to make damaging more situational like burning after igniting by performer will also counted
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ResetTimeDamageOwnerTracked = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CurrentAmount = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(required: true)]
    public DamageTrackerSpecifier DamageTracker = new();

    public float GetProgress()
    {
        if (CurrentAmount > DamageTracker.TargetAmount)
            return 1f;

        if (DamageTracker.TargetAmount == FixedPoint2.Zero)
        {
            Log.Warning("Damage tracker target amount is zero! This may occur due to admins adding it.");
            return 1f;
        }

        return (float)CurrentAmount.Value / DamageTracker.TargetAmount.Value;
    }

}

[DataDefinition]
public sealed partial class DamageTrackerSpecifier
{
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageGroupPrototype> DamageGroup;

    /// <summary>
    /// if null will count damage in all owner's mob state.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<MobState>? AllowedState;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TargetAmount = FixedPoint2.Zero;
}
