// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.SS220.CombustingMindShield;
using Robust.Shared.Random;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class MindShieldCombustionRule : StationEventSystem<MindShieldCombustionRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    protected override void Started(EntityUid uid, MindShieldCombustionRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        var queryImplants = EntityQueryEnumerator<MindShieldComponent>();
        List<EntityUid> validTargets = [];
        while (queryImplants.MoveNext(out var ent, out _))
        {
            validTargets.Add(ent);
        }

        var combustionOwner = _random.Pick(validTargets);
        if (!TryComp<ImplantedComponent>(combustionOwner, out var implanted))
            return;

        var comp = EnsureComp<CombustingMindShieldComponent>(combustionOwner);
        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (HasComp<MindShieldImplantComponent>(implant))
                comp.Implant = implant;
        }
    }
}
