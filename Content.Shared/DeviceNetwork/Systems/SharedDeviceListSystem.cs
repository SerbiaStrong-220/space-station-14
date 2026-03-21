using System.Linq;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract class SharedDeviceListSystem : EntitySystem
{
    public IEnumerable<EntityUid> GetAllDevices(EntityUid uid, DeviceListComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return new EntityUid[] { };
        }
        return component.Devices;
    }

    // SS220 add additional control for shuttle start
    public void DeleteDeviceFromList(Entity<DeviceListComponent?> ent, EntityUid device)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.Devices.Contains(device))
            return;

        ent.Comp.Devices.Remove(device);
        Dirty(ent);
    }
    // SS220 add additional control for shuttle end
}

public sealed class DeviceListUpdateEvent : EntityEventArgs
{
    public DeviceListUpdateEvent(List<EntityUid> oldDevices, List<EntityUid> devices)
    {
        OldDevices = oldDevices;
        Devices = devices;
    }

    public List<EntityUid> OldDevices { get; }
    public List<EntityUid> Devices { get; }
}

public enum DeviceListUpdateResult : byte
{
    NoComponent,
    TooManyDevices,
    UpdateOk
}
