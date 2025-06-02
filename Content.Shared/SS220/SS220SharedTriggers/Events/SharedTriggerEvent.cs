// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.SharedTriggers.SS220SharedTriggerEvent;

public sealed class SS220SharedTriggerEvent : EntityEventArgs
{
    /// <summary>
    /// The item on which the event is triggered
    /// </summary>
    public readonly EntityUid TriggeredItem;

    /// <summary>
    /// The one who activated the trigger
    /// </summary>
    public readonly EntityUid User;

    public SS220SharedTriggerEvent(EntityUid triggeredItem, EntityUid user)
    {
        TriggeredItem = triggeredItem;
        User = user;
    }
}
