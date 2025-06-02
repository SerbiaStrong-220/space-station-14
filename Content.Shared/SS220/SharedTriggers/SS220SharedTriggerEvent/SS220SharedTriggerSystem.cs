// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.SharedTriggers.SS220SharedTriggerEvent;

/// <summary>
/// public method for raises SS220SharedTriggerEvent
/// </summary>
public sealed class SS220SharedTriggerSystem : EntitySystem
{
    public void SendTrigger(EntityUid uid, EntityUid user)
    {
        var ev = new SS220SharedTriggerEvent(uid, user);
        RaiseLocalEvent(uid, ev);
    }
}
