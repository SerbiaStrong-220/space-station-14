// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Power.Components;
using Content.Server.SS220.Power;
using Content.Server.SS220.StationEvents.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Power;
using Content.Shared.SS220.Power.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.StationEvents.Events;

public sealed partial class PowerMalfunctionRule : StationEventSystem<PowerMalfunctionRuleComponent>
{
    [Dependency] private PowerMalfunctionSystem _malfunction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolarFlarePowerSurgeEvent>(OnSolarFlarePowerSurge);
    }

    protected override void Started(EntityUid uid, PowerMalfunctionRuleComponent component,
        GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var apcs = new List<Entity<PowerMalfunctionComponent>>();
        var query = EntityQueryEnumerator<ApcComponent, PowerMalfunctionComponent, TransformComponent>();
        while (query.MoveNext(out var apc, out _, out var malfunction, out var transform))
        {
            if (!IsEventTarget(apc, transform, chosenStation) || malfunction.PowerDisabled ||
                HasComp<HighPriorityAPCComponent>(apc) ||
                !TryComp<PowerNetworkBatteryComponent>(apc, out var battery) || !battery.Enabled)
                continue;

            apcs.Add((apc, malfunction));
        }

        if (apcs.Count > 0)
            _malfunction.TrySetFault(RobustRandom.Pick(apcs), true);
    }

    private void OnSolarFlarePowerSurge(ref SolarFlarePowerSurgeEvent args)
    {
        var substations = new List<Entity<PowerMalfunctionComponent>>();
        var query = EntityQueryEnumerator<PowerMalfunctionComponent, PowerMonitoringDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var device, out var malfunction, out var monitor, out var transform))
        {
            if (!IsEventTarget(device, transform))
                continue;

            if (monitor.Group == PowerMonitoringConsoleGroup.APC)
            {
                var disablePower = !HasComp<HighPriorityAPCComponent>(device) &&
                    RobustRandom.Prob(args.ApcFailureChance);
                _malfunction.TrySetFault((device, malfunction), disablePower);
            }
            else if (monitor.Group == PowerMonitoringConsoleGroup.Substation && !malfunction.PowerDisabled &&
                TryComp<PowerNetworkBatteryComponent>(device, out var battery) && battery.Enabled)
            {
                substations.Add((device, malfunction));
            }
        }

        if (substations.Count > 0)
            _malfunction.TrySetFault(RobustRandom.Pick(substations), true);
    }

    private bool IsEventTarget(EntityUid uid, TransformComponent transform, EntityUid? chosenStation = null)
    {
        return transform.Anchored && StationSystem.GetOwningStation(uid) is { } station &&
            HasComp<StationEventEligibleComponent>(station) &&
            (chosenStation == null || station == chosenStation);
    }
}

[ByRefEvent]
public readonly record struct SolarFlarePowerSurgeEvent(float ApcFailureChance);
