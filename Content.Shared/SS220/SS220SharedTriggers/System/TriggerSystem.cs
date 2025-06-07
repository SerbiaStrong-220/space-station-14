// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SS220SharedTriggers.Events;

namespace Content.Shared.SS220.SS220SharedTriggers.System;

/// <summary>
/// Public method for raises SharedTriggerEvent
/// </summary>
public sealed class TriggerSystem : EntitySystem
{
    public void TriggerTarget(EntityUid target, EntityUid? user = null)
    {
        var ev = new SharedTriggerEvent(target, user);
        RaiseLocalEvent(target, ev);
    }
}
