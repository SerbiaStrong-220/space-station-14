// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.PowerCell;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed class PathologyHealerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPathologySystem _pathology = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyHealerComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<PathologyHealerComponent, AfterInteractEvent>(OnHealingAfterInteract);

        SubscribeLocalEvent<PathologyHolderComponent, HealingPathologyDoAfterEvent>(OnDoAfter);
    }

    private void OnHealingUse(Entity<PathologyHealerComponent> healing, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(healing, args.User, args.User))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(Entity<PathologyHealerComponent> healing, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(healing, args.Target.Value, args.User))
            args.Handled = true;
    }

    private void OnDoAfter(Entity<PathologyHolderComponent> target, ref HealingPathologyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<PathologyHealerComponent>(args.Used, out var healing))
            return;

        if (!_powerCell.TryUseActivatableCharge(args.Used.Value, user: args.User))
            return;

        var damageModifier = 0f;
        foreach (var selector in healing.CurePathologyStacksSelectors)
        {
            foreach (var (id, deltaStack) in selector)
            {
                if (!_pathology.TryChangePathologyStack(target!, id, deltaStack))
                    continue;

                var modifierEv = new GetPathologyHealerDamageModifier(id, target.Owner, args.User);
                RaiseLocalEvent(target, ref modifierEv);

                damageModifier += (modifierEv.Modifier * Math.Abs(deltaStack));
                break;
            }
        }

        if (damageModifier == 0f)
            return;

        _damageable.TryChangeDamage(target.Owner, healing.DamagePerCure * damageModifier, origin: args.User, ignoreResistances: true);
    }

    private bool TryHeal(Entity<PathologyHealerComponent> healing, Entity<PathologyHolderComponent?> target, EntityUid user)
    {
        if (!Resolve(target.Owner, ref target.Comp, logMissing: false))
            return false;

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, healing.Comp.Delay, new HealingPathologyDoAfterEvent(), target, target: target, used: healing)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        return _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
