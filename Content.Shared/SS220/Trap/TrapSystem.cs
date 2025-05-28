// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// The logic of traps witch look like bears trap. Automatically “binds to leg” when activated.
/// </summary>
public sealed class TrapSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareableSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AnchorableSystem _anchorableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TrapComponent, GetVerbsEvent<AlternativeVerb>>(VerbsTrap);
        SubscribeLocalEvent<TrapComponent, InteractionTrapDoAfterEvent>(OnTrapDoAfter);
        SubscribeLocalEvent<TrapComponent, StepTriggerAttemptEvent>(EnsnareableOnStep);
    }

    private void VerbsTrap(Entity<TrapComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (_openable.IsClosed(args.Target))
            return;

        AlternativeVerb setTrap = new()
        {
            Text = Loc.GetString("trap-component-set-trap"),
        };

        AlternativeVerb defuseTrap = new()
        {
            Text = Loc.GetString("trap-component-defuse-trap"),
        };

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.InteractionDelay,
            new InteractionTrapDoAfterEvent(),
            ent.Owner,
            target: ent.Owner,
            used: args.User)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        var localUser = args.User;
        if (ent.Comp.IsArmed)
        {
            defuseTrap.Act = () => _doAfter.TryStartDoAfter(doAfterArgs);
            args.Verbs.Add(defuseTrap);
        }
        else
        {
            setTrap.Act = () =>
            {
                if (HandleSetTrap(ent.Owner, localUser))
                    _doAfter.TryStartDoAfter(doAfterArgs);
            };
            args.Verbs.Add(setTrap);
        }
    }

    private void OnTrapDoAfter(Entity<TrapComponent> ent, ref InteractionTrapDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.IsArmed) //ignore check during defuse since it is meaningless and causes errors
        {
            if (!HandleSetTrap(ent.Owner, args.User))
                return;
        }

        var xform = Transform(ent.Owner).Coordinates;
        _audio.PlayPredicted(ent.Comp.IsArmed ? ent.Comp.SetTrapSound : ent.Comp.DefuseTrapSound, xform, args.User);

        ChangeStateTrap(ent.Owner, ent.Comp);
    }

    private void EnsnareableOnStep(Entity<TrapComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (!ent.Comp.IsArmed)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.Tripper))
            return;

        if (!TryComp<StatusEffectsComponent>(args.Tripper, out var status))
            return;

        if (ent.Comp.DurationStun != TimeSpan.Zero)
        {
            _stunSystem.TryStun(args.Tripper, ent.Comp.DurationStun, true, status);
            _stunSystem.TryKnockdown(args.Tripper, ent.Comp.DurationStun, true, status);
        }

        if (ent.Comp.DamageOnTrapped != null)
        {
            if (!HasComp<DamageableComponent>(args.Tripper))
                return;
            _damageableSystem.TryChangeDamage(args.Tripper, ent.Comp.DamageOnTrapped, true);
        }

        if (ent.Comp.Reagent != null)
        {
            if (!_solutionContainers.TryGetInjectableSolution(args.Tripper, out var injectable, out _))
                return;

            _solutionContainers.TryAddReagent(injectable.Value, ent.Comp.Reagent, ent.Comp.Quantity, out _);
        }

        ChangeStateTrap(ent.Owner, ent.Comp); //now, unanchor() does not work in a container.

        if (!TryComp<EnsnaringComponent>(ent.Owner, out var ensnaring))
            return;
        _ensnareableSystem.TryEnsnare(args.Tripper, ent.Owner, ensnaring);

        if(_net.IsServer)
            _audio.PlayPvs(ent.Comp.HitTrapSound, ent.Owner);
    }

    private void UpdateVisuals(EntityUid uid,TrapComponent? trapComp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref trapComp, ref appearance, false))
            return;

        _appearance.SetData(uid, TrapVisuals.Visual,
            trapComp.IsArmed ? TrapVisuals.Armed : TrapVisuals.Unarmed, appearance);
    }

    private void ChangeStateTrap(EntityUid uid, TrapComponent comp)
    {
        comp.IsArmed = !comp.IsArmed;
        Dirty(uid,comp);
        UpdateVisuals(uid);

        if (comp.IsArmed)
            _transformSystem.AnchorEntity(uid); //клиентский баг
        else
            _transformSystem.Unanchor(uid);

        var ev = new TrapChangedArmedEvent(comp.IsArmed);
        RaiseLocalEvent(uid, ev);
    }

    private bool HandleSetTrap(EntityUid trapEntity, EntityUid user)
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return false;

        //Checking whether the trap is in hand after a successful DoAfter
        if (hands.Hands.Values.Any(h => h.HeldEntity == trapEntity))
        {
            _popup.PopupClient(Loc.GetString("trap-component-trap-in-hand"), user, user);
            return false;
        }

        //Providing a stuck traps on one tile
        var coordinates = Transform(trapEntity).Coordinates;
        if (_anchorableSystem.AnyUnstackable(trapEntity, coordinates))
        {
            _popup.PopupClient(Loc.GetString("trap-component-no-room"), user, user);
            return false;
        }
        return true;
    }
}
