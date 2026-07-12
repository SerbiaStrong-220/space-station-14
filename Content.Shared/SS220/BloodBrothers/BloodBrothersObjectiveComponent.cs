// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.BloodBrothers;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodBrothersObjectiveComponent : Component
{
    [DataField]
    public LocId Issuer = "objective-issuer-blood-brothers";

    [DataField]
    public EntityUid? BrotherObjective;
}

[ByRefEvent]
public record struct ObjectiveProgressModifyEvent(Entity<MindComponent> Mind, float Progress);
