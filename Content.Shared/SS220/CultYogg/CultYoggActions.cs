// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

public sealed partial class CultYoggPukeShroomEvent : InstantActionEvent
{
}

public sealed partial class CultYoggDigestEvent : InstantActionEvent
{
}
public sealed partial class CultYoggCorruptItemEvent : EntityTargetActionEvent
{
}

public sealed partial class CultYoggCorruptItemInHandEvent : InstantActionEvent
{
}

public sealed partial class CultYoggAscendingEvent : InstantActionEvent
{
}

