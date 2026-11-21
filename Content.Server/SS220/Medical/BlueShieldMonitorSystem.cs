// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Access.Systems;
using Content.Shared.Implants.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.Medical;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;

namespace Content.Server.SS220.Medical;

public sealed class BlueShieldMonitorSystem : EntitySystem
{
    private const string UnknownNameLocKey = "suit-sensor-component-unknown-name";
    private const string UnknownJobLocKey = "suit-sensor-component-unknown-job";
    private const string NoIdJobIcon = "JobIconNoId";

    [Dependency] private SharedIdCardSystem _idCards = default!;
    [Dependency] private EntityWhitelistSystem _whitelists = default!;
    [Dependency] private SharedMindSystem _minds = default!;
    [Dependency] private SharedJobSystem _jobs = default!;

    /// <summary>
    ///     Prepares suit sensor statuses for a crew monitoring console.
    ///     Blue Shield monitors receive a filtered list: high clearance personnel,
    ///     agents disguised as heads (chameleon) and unidentified crew without ID cards.
    ///     Regular consoles receive the full list with the SS220-only fields stripped,
    ///     so modified clients cannot use them to reveal disguised agents.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ProcessSensorStatus(EntityUid uid, Dictionary<string, SuitSensorStatus> sensors)
    {
        if (TryComp<BlueShieldMonitorComponent>(uid, out var blueShieldMonitor))
        {
            return FilterBlueShieldSensors(uid, sensors);
        }

        return StripSensitiveSensorData(sensors);
    }

    private Dictionary<string, SuitSensorStatus> FilterBlueShieldSensors(
        EntityUid monitorUid,
        Dictionary<string, SuitSensorStatus> sensors)
    {
        if (!TryComp<BlueShieldMonitorComponent>(monitorUid, out var comp))
            return new Dictionary<string, SuitSensorStatus>();

        var filtered = new Dictionary<string, SuitSensorStatus>();

        foreach (var (address, sensor) in sensors)
        {
            // 1. Genuine high-ranking personnel.
            // Two independent sources are accepted:
            //   - the job role on the person's mind, assigned by the server and impossible to forge;
            //   - the JobPrototype of their ID card, but only when that card is not an agent ID card.
            // Agent ID cards let their owner freely rewrite the card's job title, color and prototype,
            // so their card data is never trusted here. Disguised agents are caught by rule 2 instead.
            if (IsHighClearanceMind(sensor.OwnerUid, comp) || IsTrustedHighClearanceCard(sensor, comp))
            {
                filtered.Add(address, sensor);
                continue;
            }

            // 2. Agent disguised as a head (chameleon)
            if (sensor.IsAgentIdCard && comp.HighClearanceIcons.Contains(sensor.JobIcon))
            {
                filtered.Add(address, sensor);
                continue;
            }

            // 3. Unidentified person: tracked by an implant and carrying no ID card right now.
            if (IsUnidentified(comp, sensor))
                filtered.Add(address, MaskIdentity(sensor));
        }

        return filtered;
    }

    /// <summary>
    ///     Returns true if the person behind the sensor status has a high clearance job
    ///     according to their mind. People without a mind (NPCs, dummies) never pass,
    ///     so they can only be caught by the icon-based rule instead.
    /// </summary>
    private bool IsHighClearanceMind(NetEntity ownerUid, BlueShieldMonitorComponent comp)
    {
        if (!TryGetEntity(ownerUid, out var owner))
            return false;

        if (!_minds.TryGetMind(owner.Value, out var mindId, out _))
            return false;

        if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
            return false;

        return comp.HighClearanceProtos.Contains(jobId.Value.Id.ToLowerInvariant());
    }

    /// <summary>
    ///     Returns true if the sensor reports a high clearance job prototype on a trustworthy ID card.
    ///     Agent ID cards are never trustworthy: their owner can rewrite the card's job prototype at will.
    ///     <see cref="SuitSensorStatus.IsAgentIdCard"/> is computed server-side from the presence of
    ///     AgentIDCardComponent, so it cannot be spoofed by a modified client.
    /// </summary>
    private static bool IsTrustedHighClearanceCard(SuitSensorStatus sensor, BlueShieldMonitorComponent comp)
    {
        if (sensor.IsAgentIdCard)
            return false;

        return comp.HighClearanceProtos.Contains(sensor.JobPrototypeId.ToLowerInvariant().Trim());
    }

    /// <summary>
    ///     Returns a copy of the status with the identity fields replaced by the same
    ///     "unknown" values the suit sensors use when no ID card was found.
    /// </summary>
    private SuitSensorStatus MaskIdentity(SuitSensorStatus sensor)
    {
        return new SuitSensorStatus(
            sensor.OwnerUid,
            sensor.SuitSensorUid,
            Loc.GetString(UnknownNameLocKey),
            Loc.GetString(UnknownJobLocKey),
            NoIdJobIcon,
            new List<string>(),
            string.Empty,
            isAgentIdCard: false)
        {
            Timestamp = sensor.Timestamp,
            IsAlive = sensor.IsAlive,
            TotalDamage = sensor.TotalDamage,
            TotalDamageThreshold = sensor.TotalDamageThreshold,
            Coordinates = sensor.Coordinates,
        };
    }

    /// <summary>
    ///     An unidentified person is someone who is currently implanted with a tracking implant
    ///     and does not have any ID card on them at the moment of the check.
    ///     Both conditions are verified live, so a person appears in the list only while
    ///     they are implanted and ID-less, and leaves it as soon as they get an ID card
    ///     or the implant is removed.
    /// </summary>
    private bool IsUnidentified(BlueShieldMonitorComponent comp, SuitSensorStatus status)
    {
        if (!TryGetEntity(status.OwnerUid, out var owner))
            return false;

        if (!IsTrackedByImplant(comp, status, owner.Value))
            return false;

        return !_idCards.TryFindIdCard(owner.Value, out _);
    }

    /// <summary>
    ///     Checks that the status was reported by a tracking implant which is still inside the person
    ///     the status is reported for.
    /// </summary>
    private bool IsTrackedByImplant(BlueShieldMonitorComponent comp, SuitSensorStatus status, EntityUid owner)
    {
        if (!TryGetEntity(status.SuitSensorUid, out var sensor))
            return false;

        if (!_whitelists.IsWhitelistPass(comp.TrackingImplantWhitelist, sensor.Value))
            return false;

        if (!TryComp<SubdermalImplantComponent>(sensor.Value, out var implant) || implant.ImplantedEntity == null)
            return false;

        return implant.ImplantedEntity.Value == owner;
    }

    private static Dictionary<string, SuitSensorStatus> StripSensitiveSensorData(Dictionary<string, SuitSensorStatus> sensors)
    {
        var stripped = new Dictionary<string, SuitSensorStatus>(sensors.Count);

        foreach (var (address, sensor) in sensors)
        {
            stripped[address] = new SuitSensorStatus(
                sensor.OwnerUid,
                sensor.SuitSensorUid,
                sensor.Name,
                sensor.Job,
                sensor.JobIcon,
                sensor.JobDepartments,
                string.Empty,
                isAgentIdCard: false)
            {
                Timestamp = sensor.Timestamp,
                IsAlive = sensor.IsAlive,
                TotalDamage = sensor.TotalDamage,
                TotalDamageThreshold = sensor.TotalDamageThreshold,
                Coordinates = sensor.Coordinates,
            };
        }

        return stripped;
    }
}
