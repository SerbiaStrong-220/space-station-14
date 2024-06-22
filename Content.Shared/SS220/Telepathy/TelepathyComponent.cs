using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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

    [DataField("canSend", required: true)]
    public bool CanSend;

    [DataField("telepathyChannelPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TelepathyChannelPrototype>))]
    public string TelepathyChannelPrototype;
}

public sealed partial class TelepathyActionEvent : InstantActionEvent
{

}

public sealed partial class TelepathySaidEvent : InstantActionEvent
{
    public string Message { get; init; }
}
