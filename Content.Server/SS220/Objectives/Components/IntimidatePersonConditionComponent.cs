// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Trackers.Components;

namespace Content.Server.SS220.Objectives.Components;

[RegisterComponent]
public sealed partial class IntimidatePersonConditionComponent : Component
{
    [DataField(required: true)]
    public DamageTrackerSpecifier DamageTrackerSpecifier = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid TargetMob;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ObjectiveIsDone = false;
}
