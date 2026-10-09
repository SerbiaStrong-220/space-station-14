

using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Radio;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem : EntitySystem
{

    public bool TryGetFrequencyRadioChannel(Entity<InventoryComponent?> source, [NotNullWhen(true)] out RadioChannelPrototype? channel, [NotNullWhen(true)] out FixedPoint2? frequency)
    {
        channel = null;
        frequency = null;
        var ev = new GetFrequencyRadioEvent();

        if (!Resolve(source.Owner, ref source.Comp))
            return false;

        RaiseLocalEvent(source.AsNullable(), ref ev);

        if (ev.Channel is null || ev.Frequency is null)
            return false;

        channel = ev.Channel;
        frequency = ev.Frequency;

        return true;
    }
}


/// <summary>
///
/// </summary>
[ByRefEvent]
public record struct GetFrequencyRadioEvent(RadioChannelPrototype? Channel = null, FixedPoint2? Frequency = null) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}
