// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.PathologyStatusEffects;

public sealed partial class PathologyDamageStatusEffectSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<PathologyDamageStatusEffectComponent, PathologyStatusEffectStackableComponent, StatusEffectComponent>();
        while (query.MoveNext(out var damageEffect, out var stackable, out var statusEffect))
        {
            if (_timing.CurTime < damageEffect.NextUpdate)
                continue;

            damageEffect.NextUpdate = _timing.CurTime + UpdateInterval;

            if (statusEffect.AppliedTo is not { } target || _mobState.IsDead(target))
                continue;

            var stacks = Math.Min(stackable.StackCount, damageEffect.MaxStacks);
            if (stacks <= 0)
                continue;

            _damageable.TryChangeDamage(target, damageEffect.Damage * stacks, ignoreResistances: true, interruptsDoAfters: false);
        }
    }
}
