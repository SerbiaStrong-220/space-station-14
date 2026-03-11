// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Mind;

namespace Content.Shared.FCB.Mind.Systems;

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
