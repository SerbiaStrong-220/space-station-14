// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// This is used for giving telepathy ability
/// </summary>
[RegisterComponent]
public sealed partial class TelepathyComponent : Component
{
    [DataField("canSend", required: true)]
    public bool CanSend;

    [DataField("telepathyChannelPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TelepathyChannelPrototype>))]
    public string TelepathyChannelPrototype;
}

public sealed partial class TelepathySendEvent : InstantActionEvent
{
    public string Message { get; init; }
}

public sealed partial class TelepathyAnnouncementSendEvent : InstantActionEvent
{
    public string Message { get; init; }
    public string TelepathyChannel { get; init; }
}
