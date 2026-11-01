// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;

namespace Content.Shared.Medical.SuitSensors;

public abstract partial class SharedSuitSensorSystem : EntitySystem
{
    private bool IsSuppressedByWornSensor(EntityUid wearer)
    {
        var slotEnumerator = _inventory.GetSlotEnumerator(wearer, SlotFlags.All);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<SuitSensorComponent>(item, out var sensor)
                && sensor.Mode != SuitSensorMode.SensorOff
                && sensor.User == wearer)
            {
                return true;
            }
        }

        return false;
    }
}

