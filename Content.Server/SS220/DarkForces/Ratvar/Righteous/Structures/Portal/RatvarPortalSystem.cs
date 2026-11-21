using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Events;
using Content.Shared.Destructible;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Portal;

public sealed class RatvarPortalSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarPortalComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RatvarPortalComponent, DestructionEventArgs>(OnDestroy);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<RatvarPortalComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.RatvarSpawnTick > curTime)
                continue;

            var ratvar = Spawn("MobRatvarDark", Transform(uid).Coordinates);
            var ev = new RatvarSpawnedEvent(ratvar);
            RaiseLocalEvent(ref ev);
            QueueDel(uid);
        }
    }

    private void OnComponentInit(EntityUid uid, RatvarPortalComponent component, ComponentInit args)
    {
        component.RatvarSpawnTick = _gameTiming.CurTime + TimeSpan.FromMinutes(3);
        var ev = new RatvarSpawnStartedEvent(uid);
        RaiseLocalEvent(ref ev);
    }

    private void OnDestroy(EntityUid uid, RatvarPortalComponent component, DestructionEventArgs args)
    {
        var ev = new RatvarSpawnCanceledEvent();
        RaiseLocalEvent(ref ev);
    }
}
