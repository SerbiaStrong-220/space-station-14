// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using JetBrains.Annotations;

namespace Content.Shared.Delivery;

public sealed partial class DeliveryModifierSystem
{
    [PublicAPI]
    public TimeSpan GetTimeLeft(EntityUid uid)
    {
        if (!TryComp<DeliveryPriorityComponent>(uid, out var priority))
            return TimeSpan.Zero;

        var timeLeft = priority.DeliverUntilTime - _timing.CurTime;
        return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
    }

    [PublicAPI]
    public void SetTimeLeft(EntityUid uid, TimeSpan timeLeft)
    {
        if (!TryComp<DeliveryPriorityComponent>(uid, out var priority))
            return;

        priority.DeliverUntilTime = _timing.CurTime + timeLeft;
        priority.Expired = false;
        priority.Delivered = false;

        _delivery.UpdatePriorityVisuals((uid, priority));
        Dirty(uid, priority);
    }
}

