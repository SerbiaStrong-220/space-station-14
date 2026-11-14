using Content.Server.SS220.DarkForces.Narsi.Runes.Events;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Components;
using Content.Server.Afk;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain;

public sealed partial class ChaplainSystem
{
    [Dependency] private readonly IAfkManager _afkManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private void InitializeNarsi()
    {
        SubscribeLocalEvent<NarsiSummoningEndEvent>(OnNarsiSummoning);
    }

    private void OnNarsiSummoning(NarsiSummoningEndEvent args)
    {
        if (args.Cancelled)
            return;

        var chaplains = EntityQueryEnumerator<ChaplainComponent>();
        if (!TryGetActiveChaplain(chaplains, out var chaplain, out var component))
            return;

        if (chaplain == null || component == null)
            return;

        _actions.AddAction(chaplain.Value, ref component.NarsiExileActionEntity, component.NarsiExileAction);
        RaiseLocalEvent(new ChaplainNarsiExileEnableEvent(chaplain.Value));
    }

    private bool TryGetActiveChaplain(EntityQueryEnumerator<ChaplainComponent> chaplains, out EntityUid? chaplain, out ChaplainComponent? chaplainComponent)
    {
        while (chaplains.MoveNext(out var uid, out var component))
        {
            if (!_mobStateSystem.IsAlive(uid) || !TryComp<ActorComponent>(uid, out var actorComponent))
                continue;

            if (_afkManager.IsAfk(actorComponent.PlayerSession))
                continue;

            chaplain = uid;
            chaplainComponent = component;

            return true;
        }

        chaplain = null;
        chaplainComponent = null;

        return false;
    }
}
