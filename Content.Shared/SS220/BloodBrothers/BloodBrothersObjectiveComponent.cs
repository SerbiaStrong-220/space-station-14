using Content.Shared.Mind;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.BloodBrothers;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodBrothersObjectiveComponent : Component
{
    [DataField]
    public string Issuer = "objective-issuer-blood-brothers";

    [DataField]
    public EntityUid? BrotherObjective;
}

[ByRefEvent]
public record struct ObjectiveProgressModifyEvent(Entity<MindComponent> Mind, float Progress);
