// © SS220, An EULA/CLA with a hosting restriction, full text: https://githubusercontent.com

using Content.Shared.SS220.Medical;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Medical;

public sealed class BlueShieldMonitorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlueShieldMonitorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<BlueShieldMonitorComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnUIOpened(EntityUid uid, BlueShieldMonitorComponent component, BoundUIOpenedEvent args)
    {
        var emptyState = new CrewMonitoringState(new List<SuitSensorStatus>());
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, emptyState);
    }

    private void OnPacketReceived(EntityUid uid, BlueShieldMonitorComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) ||
            command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!args.Data.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        var filteredEntries = new List<SuitSensorStatus>();

        foreach (var (_, sensor) in sensorStatus)
        {
            var jobLower = sensor.Job.ToLower().Trim();
            var nameLower = sensor.Name.ToLower().Trim();

            if (jobLower == "н/д" || nameLower == "неизвестно" || string.IsNullOrWhiteSpace(sensor.Job))
            {
                filteredEntries.Add(sensor);
                continue;
            }

            if (jobLower.Contains("капитан") ||
                jobLower.Contains("глава персонала") ||
                jobLower.Contains("представитель нанотрейзен") ||
                jobLower.Contains("старший инженер") ||
                jobLower.Contains("главный врач") ||
                jobLower.Contains("глава службы безопасности") ||
                jobLower.Contains("квартирмейстер") ||
                jobLower.Contains("научный руководитель") ||
                jobLower.Contains("синий щит") ||
                jobLower.Contains("врио"))
            {
                filteredEntries.Add(sensor);
            }
        }

        var uiState = new CrewMonitoringState(filteredEntries);
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, uiState);
    }
}
