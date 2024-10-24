// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Trackers.Components;

namespace Content.Server.SS220.Objectives.Components;

[RegisterComponent]
public sealed partial class FramePersonConditionComponent : Component
{
    [DataField(required: true)]
    public CriminalStatusTrackerSpecifier CriminalStatusSpecifier = new();

    public bool ObjectiveIsDone = false;
}
