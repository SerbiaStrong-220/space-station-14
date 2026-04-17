// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class GravityGeneratorAnomalyRule : StationEventSystem<GravityGeneratorAnomalyRuleComponent>
{
    [Dependency] private readonly PowerChargeSystem _powerCharge = default!;

    protected override void Started(EntityUid uid, GravityGeneratorAnomalyRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        component.AffectedStation = chosenStation.Value;

        var query = AllEntityQuery<GravityGeneratorComponent, PowerChargeComponent, TransformComponent>();
        while (query.MoveNext(out var generatorUid, out var _, out var powerChargeComponent, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation)
                _powerCharge.ClearCharge((generatorUid, powerChargeComponent), uid);
        }
    }

    protected override void Ended(EntityUid uid, GravityGeneratorAnomalyRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        var query = AllEntityQuery<GravityGeneratorComponent, PowerChargeComponent, TransformComponent>();
        while (query.MoveNext(out var generatorUid, out var _, out var powerChargeComponent, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == component.AffectedStation)
                _powerCharge.SetSwitchOn((generatorUid, powerChargeComponent), uid);
        }
    }
}

