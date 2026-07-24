// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Mind;

namespace Content.Shared.SS220.Mind;

[ByRefEvent]
public readonly record struct EntityVisitedEvent(EntityUid MindEntity, MindComponent MindComp)
{
    public readonly EntityUid MindEntity = MindEntity;

    public readonly MindComponent MindComp = MindComp;
}

[ByRefEvent]
public readonly record struct EntityUnvisitedEvent(EntityUid MindEntity, MindComponent MindComp)
{
    public readonly EntityUid MindEntity = MindEntity;

    public readonly MindComponent MindComp = MindComp;
}
