using Content.Shared.FixedPoint;
using Content.Shared.Mobs;

namespace Content.Shared.SS220.ModifyThreshold;

[RegisterComponent]
public sealed partial class ModifyThresholdComponent : Component
{
    [DataField]
    public float Duration;

    [DataField]
    public Dictionary<FixedPoint2, MobState> NewThresholds = [];

    public Dictionary<FixedPoint2, MobState> OldThresholds = [];
    public bool IsChanged = false;
}
