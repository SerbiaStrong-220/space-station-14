using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Felinid;

public sealed partial class FelinidDashActionEvent : InstantActionEvent
{
}

public sealed partial class FelinidPipecrawlActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class FelinidPipeExtractionDoAfterEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public readonly record struct FelinidPipecrawlVisualsChangedEvent(bool Active);
