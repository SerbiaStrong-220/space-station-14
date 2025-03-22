// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public sealed class LimitationReviveSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, UpdateMobStateEvent>(OnDeadMobState);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<LimitationReviveComponent, RejuvenateEvent>(OnUseAdminCommand);
    }

    private void OnDeadMobState(Entity<LimitationReviveComponent> ent, ref UpdateMobStateEvent args)
    {
        if(args.State != MobState.Dead)
            return;

        if(ent.Comp.IsAlreadyDead)
            return;

        ent.Comp.IsAlreadyDead = true;

        if(!TryComp<DamageableComponent>(ent.Owner, out var damageComp))
            return;

        _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.TypeDamageOnDead, true);
        ent.Comp.CounterOfDead++;
    }

    private void OnCloning(Entity<LimitationReviveComponent> ent, ref CloningEvent args)
    {
        ent.Comp.IsAlreadyDead = false;
        ent.Comp.CounterOfDead = 0;
    }

    private void OnUseAdminCommand(Entity<LimitationReviveComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.IsAlreadyDead = false;
        ent.Comp.CounterOfDead = 0;
    }
}
