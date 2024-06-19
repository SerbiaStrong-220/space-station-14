using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// This is used for giving telepathy ability
/// </summary>
[RegisterComponent]
public sealed partial class TelepathyComponent : Component
{
    [DataField]
    public EntProtoId TelepathyAction = "ActionTelepathy";

    [DataField]
    public EntityUid? TelepathyActionEntity;
}

public sealed partial class TelepathyActionEvent : InstantActionEvent
{

}
