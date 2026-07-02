// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pathology;

public sealed partial class VirusOutbreakRuleSystem : StationEventSystem<VirusOutbreakRuleComponent>
{
    [Dependency] private SharedPathologySystem _pathology = default!;

    protected override void Started(EntityUid uid, VirusOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var viruses = new List<ProtoId<VirusPrototype>>();
        foreach (var virus in PrototypeManager.EnumeratePrototypes<VirusPrototype>())
            viruses.Add(virus.ID);

        if (viruses.Count == 0)
            return;

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<PathologyHolderComponent, MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var ent, out _, out var mind, out _))
        {
            if (mind.HasMind && !HasComp<VirusImmunityComponent>(ent))
                candidates.Add(ent);
        }

        if (candidates.Count == 0)
            return;

        var victims = Math.Min(RobustRandom.Next(component.MinVictims, component.MaxVictims + 1), candidates.Count);
        for (var i = 0; i < victims; i++)
            _pathology.TryAddVirus(RobustRandom.PickAndTake(candidates), RobustRandom.Pick(viruses), out _);
    }
}
