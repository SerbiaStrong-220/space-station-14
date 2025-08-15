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
    }
    private void StartCollide(EntityUid uid, HealOnCollideComponent comp, ref StartCollideEvent ev)
    {
        Log.Debug("0000");
        if (!comp.Healed.TryGetValue(ev.OtherEntity, out var timeSpan))
        {
            comp.Healed.Add(ev.OtherEntity, _gameTiming.CurTime);
            _damageableSystem.TryChangeDamage(ev.OtherEntity, comp.Heal);
            if (comp.StopBlooding)
                _bloodstreamSystem.TryModifyBleedAmount(ev.OtherEntity, comp.BloodlossModifier);
            return;
        }
        Log.Debug((timeSpan + TimeSpan.FromSeconds(comp.Cooldown) > _gameTiming.CurTime).ToString());
        Log.Debug(timeSpan.ToString());
        if (timeSpan + TimeSpan.FromSeconds(comp.Cooldown) > _gameTiming.CurTime) return;
        comp.Healed[ev.OtherEntity] = _gameTiming.CurTime;
        _damageableSystem.TryChangeDamage(ev.OtherEntity, comp.Heal);
        if (comp.StopBlooding)
            _bloodstreamSystem.TryModifyBleedAmount(ev.OtherEntity, comp.BloodlossModifier);
    }
}
