// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// сервер потому что BloodstreamSystem в сервере

using Content.Shared.SS220.HealOnCollide.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Server.Body.Systems;

namespace Content.Server.SS220.HealOnCollide.Systems;

public sealed class HealOnCollideSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealOnCollideComponent, StartCollideEvent>(StartCollide);
        SubscribeLocalEvent<HealOnCollideComponent, EndCollideEvent>(EndCollide);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HealOnCollideComponent>();
        while (query.MoveNext(out var _, out var comp))
        {
            foreach (var other in comp.Collided)
            {
                if (comp.Healed.TryGetValue(other, out var timeSpan))
                {
                    if (timeSpan + TimeSpan.FromSeconds(comp.Cooldown) > _gameTiming.CurTime)
                        continue;
                    comp.Healed[other] = _gameTiming.CurTime;
                }
                else
                    comp.Healed.Add(other, _gameTiming.CurTime);
                _damageableSystem.TryChangeDamage(other, comp.Heal);
                if (comp.StopBlooding)
                    _bloodstreamSystem.TryModifyBleedAmount(other, comp.BloodlossModifier);
            }
        }
    }
    private void StartCollide(EntityUid uid, HealOnCollideComponent comp, ref StartCollideEvent ev)
    {
        comp.Collided.Add(ev.OtherEntity);
    }
    private void EndCollide(EntityUid uid, HealOnCollideComponent comp, ref EndCollideEvent ev)
    {
        comp.Collided.Remove(ev.OtherEntity);
    }
}
