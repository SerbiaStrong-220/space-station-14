using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.PowerCell;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        //SS220-new-feature begin
        if (HasComp<Content.Shared.SS220.Medical.BlueShieldMonitorComponent>(uid))
        {
            sensorStatus = FilterBlueShieldSensors(sensorStatus);
        }
        //SS220-new-feature end

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    //SS220-new-feature begin
    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(Dictionary<string, SuitSensorStatus> sensors)
    {
        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            var jobLower = sensor.Job.ToLower().Trim();
            var nameLower = sensor.Name.ToLower().Trim();

            if (jobLower == "н/д" || nameLower == "неизвестно" || string.IsNullOrWhiteSpace(sensor.Job))
            {
                filtered.Add(address, sensor);
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
                filtered.Add(address, sensor);
            }
        }

        return filtered;
    }
    //SS220-new-feature end
}
