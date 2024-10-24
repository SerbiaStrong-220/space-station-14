// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.CQCCombat;
using Content.Shared.SS220.UseableBook;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Server.Hands.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffect;
using Content.Shared.Bed.Sleep;
using Content.Shared.Stunnable;
using Content.Shared.Humanoid;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Content.Shared.Zombies;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server.SS220.CQCCombat;

public sealed class CQCCombatSystem : CQCCombatSharedSystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    private const string StatusEffectKey = "ForcedSleep"; // Same one used by N2O and other sleep chems.
    private const double SleepCooldown = 30;
    private const double BlowbackParalyze = 4;
    public override void Initialize()
    {
        SubscribeLocalEvent<CQCCanReadBook>(CanReadCQCBook);
        SubscribeLocalEvent<CQCCombatComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CQCCombatComponent, CQCBlowbackEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCPunchEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCDisarmEvent>(BaseAction);
        SubscribeLocalEvent<CQCCombatComponent, CQCLongSleepEvent>(BaseAction);
    }
    private void CanReadCQCBook(CQCCanReadBook args)
    {
        args.Handled = true;
        if (HasComp<CQCCombatComponent>(args.Interactor))
        {
            args.Can = false;
            args.Cancelled = true;
            args.Reason = Loc.GetString("cqc-cannotlearn");
            return;
        }
        args.Can = true;
    }

    private void OnComponentInit(EntityUid uid, CQCCombatComponent component, ComponentInit args)
    {
        foreach (var proto in component.AvailableSpells)
        {
            var action = _actions.AddAction(uid, _prototypeManager.Index<CQCCombatSpellPrototype>(proto.Id).Entity);
            if (action is not null)
            {
                var comp = AddComp<CQCCombatInfosComponent>(action.Value);
                comp.Prototype = proto.Id;
            }
        }
    }

    private EntityUid? GetTarget(EntityUid inflictor, BaseActionEvent args)
    {
        if (args is EntityTargetActionEvent actionEvent)
            return actionEvent.Target;
        if (args is InstantActionEvent)
        {
            if (TryComp<PullerComponent>(inflictor, out var puller))
                return puller.Pulling;
        }
        return null;
    }

    private CQCCombatSpellPrototype? GetSpell(EntityUid? action)
    {
        if (action is null)
            return null;
        if (!TryComp<CQCCombatInfosComponent>(action, out var infosComponent))
            return null;

        foreach (var spell in _prototypeManager.EnumeratePrototypes<CQCCombatSpellPrototype>())
            if (infosComponent.Prototype == spell.ID)
                return spell;
        return null;
    }

    private void BaseAction(EntityUid inflictor, CQCCombatComponent comp, BaseActionEvent args)
    {
        if (HasComp<ZombieComponent>(inflictor))
        {
            args.Handled = true;
            return;
        }
        if ((GetTarget(args.Performer, args) is { } target) && !HasComp<CQCCombatComponent>(target))
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
            }

            args.Handled = true;
            ApplyDamage(target, GetSpell(args.Action)?.Damage);

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
