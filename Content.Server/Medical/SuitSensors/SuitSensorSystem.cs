using Content.Server.Access.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private SharedIdCardSystem _idCardSystem = default!; //SS220-suit-sensor-job-filter

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            // check if sensor is ready to update
            if (curTime < sensor.NextUpdate)
                continue;
            sensor.NextUpdate += sensor.UpdateRate;

            if (!CheckSensorAssignedStation((uid, sensor)))
                continue;

            // get sensor status
            var status = GetSensorState((uid, sensor));
            if (status == null)
                continue;

            // Check if the ID card is an Agent ID card
            status.IsAgentIdCard = CheckAgentIdCard(sensor.User); //SS220-suit-sensor-job-filter

            //Retrieve active server address if the sensor isn't connected to a server
            if (sensor.ConnectedServer == null)
            {
                if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(sensor.StationId!.Value, out var address))
                    continue;

                sensor.ConnectedServer = address;
            }

            // Send it to the connected server
            var payload = SuitSensorToPacket(status);

            // Clear the connected server if its address isn't on the network
            if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, sensor.ConnectedServer))
            {
                sensor.ConnectedServer = null;
                continue;
            }

            _deviceNetworkSystem.QueuePacket(uid, sensor.ConnectedServer, payload, device: device);
        }
    }

    //SS220-suit-sensor-job-filter begin
    /// <summary>
    ///     Checks if the user of the sensor is wearing an Agent ID card.
    ///     Returns true if the ID card entity has an AgentIDCardComponent.
    /// </summary>
    private bool CheckAgentIdCard(EntityUid? userUid)
    {
        if (userUid == null || !userUid.HasValue)
            return false;

        // Find the ID card (hands, entity, or inventory "id" slot)
        if (!_idCardSystem.TryFindIdCard(userUid.Value, out var card))
            return false;

        // Check if the card entity has AgentIDCardComponent
        return TryComp<AgentIDCardComponent>(card.Owner, out _);
    }
    //SS220-suit-sensor-job-filter end
}
