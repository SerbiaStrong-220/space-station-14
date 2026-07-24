using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Felinid;

public sealed partial class DashSpeedModifierActionEvent : InstantActionEvent
{
}

public sealed partial class DisposalPipeCrawlerActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DisposalPipeExtractionDoAfterEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public readonly record struct DisposalPipeCrawlerVisualsChangedEvent(bool InsidePipe);
