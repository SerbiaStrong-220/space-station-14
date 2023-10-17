using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Random;

namespace Content.Server.SS220.EvilTwin.StationEvents;
public sealed partial class EvilTwinRule : StationEventSystem<EvilTwinRuleComponent>
{
    protected override void Started(EntityUid uid, EvilTwinRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if(!_evilTwinSystem.MakeTwin(out _))
        {
            Sawmill.Warning("Map not have latejoin spawnpoints for creating evil twin spawner");
        }
    }

    [Dependency] private readonly EvilTwinSystem _evilTwinSystem = default!;
}
