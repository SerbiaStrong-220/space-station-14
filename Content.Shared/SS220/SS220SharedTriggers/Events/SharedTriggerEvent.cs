// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.SS220SharedTriggers.Events;

public sealed class SharedTriggerEvent : EntityEventArgs
{
    /// <summary>
    /// The item on which the event is triggered
    /// </summary>
    public readonly EntityUid TriggeredItem;

    /// <summary>
    /// The entity that activated the trigger
    /// </summary>
    public readonly EntityUid? Activator;

    public SharedTriggerEvent(EntityUid triggeredItem, EntityUid? activator)
    {
        TriggeredItem = triggeredItem;
        Activator = activator;
    }
}
