// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.CQCCombat;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Server.Hands.Systems;
using Content.Shared.Pulling.Components;
using Content.Server.Bed.Sleep;
using Content.Shared.StatusEffect;
using Content.Shared.Bed.Sleep;
using Content.Shared.Stunnable;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.CQCCombat;

public sealed class CQCCombatSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    private const string StatusEffectKey = "ForcedSleep"; // Same one used by N2O and other sleep chems.
    private const double SleepCooldown = 120;
    private const double BlowbackParalyze = 6;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CQCCombatComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CQCCombatComponent, CQCBlowbackEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCPunchEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCDisarmEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCLongSleepEvent>(BaseAction);
    }

    private void OnComponentInit(EntityUid uid, CQCCombatComponent component, ComponentInit args)
    {
        foreach (var proto in component.AvailableSpells)
            _actions.AddAction(uid, _prototypeManager.Index<CQCCombatSpellPrototype>(proto.Id).Entity);
    }

    private EntityUid? GetTarget(EntityUid inflictor, BaseActionEvent args)
    {
        if (args is EntityTargetActionEvent actionEvent)
            return actionEvent.Target;
        if (args is InstantActionEvent instaAction)
        {
            if (TryComp<SharedPullerComponent>(inflictor, out var puller))
                return puller.Pulling;
        }
        return null;
    }

    private CQCCombatSpellPrototype? GetSpell(EntityUid action)
    {
        foreach (var spell in _prototypeManager.EnumeratePrototypes<CQCCombatSpellPrototype>())
            if (TryComp<MetaDataComponent>(action, out var metaDataComponent))
                if (spell.Entity == metaDataComponent.EntityPrototype?.ID)
                    return spell;
        return null;
    }

    private void BaseAction(EntityUid inflictor, CQCCombatComponent comp, BaseActionEvent args)
    {
        if (GetTarget(args.Performer, args) is { } target)
        {
            if (!HasComp<HumanoidAppearanceComponent>(target))
                return;
            switch (args)
            {
                case CQCBlowbackEvent:
                    OnBlowback(args.Performer, target);
                    break;
                case CQCDisarmEvent:
                    OnDisarm(args.Performer, target);
                    break;
                case CQCLongSleepEvent:
                    OnLongSleep(args.Performer, target);
                    break;
                default:
                    return;
            }

            args.Handled = true;
            ApplyDamage(target, GetSpell(inflictor)?.Damage);
            
            return;
        }

        // Notify when there are no target
    }

    private void ApplyDamage(EntityUid target, DamageSpecifier? damage)
    {
        if (damage is null)
            return;

        _damage.TryChangeDamage(target, damage);
    }

    private void OnBlowback(EntityUid inflictor, EntityUid target)
    {
        _stun.TryParalyze(target, TimeSpan.FromSeconds(BlowbackParalyze), true);
    }

    private void OnDisarm(EntityUid inflictor, EntityUid target)
    {
        if (TryComp<HandsComponent>(target, out var handsComponent))
            foreach (var kvp in handsComponent.Hands)
                _hands.TryDrop(target, kvp.Value, null, false, false, handsComponent);
    }

    private void OnLongSleep(EntityUid inflictor, EntityUid target)
    {
        _sleeping.TrySleeping(target);
        _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(target, StatusEffectKey,
                TimeSpan.FromSeconds(SleepCooldown), true);
    }
}
