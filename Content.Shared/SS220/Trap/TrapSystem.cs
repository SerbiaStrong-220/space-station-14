// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Construction.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.SS220SharedTriggers.Events;
using Content.Shared.SS220.SS220SharedTriggers.System;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// The logic of traps witch look like bears trap. Automatically “binds to leg” when activated.
/// </summary>
public sealed class TrapSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareableSystem = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AnchorableSystem _anchorableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TrapComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeSetTrap);
        SubscribeLocalEvent<TrapComponent, TrapDefuseDoAfterEvent>(OnDefuseDoAfter);
        SubscribeLocalEvent<TrapComponent, TrapSetDoAfterEvent>(OnSetDoAfter);
        SubscribeLocalEvent<TrapComponent, StartCollideEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<TrapComponent, SharedTriggerEvent>(OnTrigger);
    }

    private void OnAlternativeSetTrap(Entity<TrapComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (_openable.IsClosed(args.Target))
            return;

        var verb = new AlternativeVerb();
        var localUser = args.User;
        if (ent.Comp.IsArmed)
        {
            var defuseDoAfter = new DoAfterArgs(
                EntityManager,
                args.User,
                ent.Comp.DefuseTrapDelay,
                new TrapDefuseDoAfterEvent(),
                ent.Owner,
                target: ent.Owner,
                used: args.User)
            {
                BreakOnMove = true,
                AttemptFrequency = AttemptFrequency.StartAndEnd,
            };

            verb.Text = Loc.GetString("trap-component-defuse-trap");
            verb.Act = () => _doAfter.TryStartDoAfter(defuseDoAfter);
        }
        else
        {
            var setDoAfter = new DoAfterArgs(
                EntityManager,
                args.User,
                ent.Comp.SetTrapDelay,
                new TrapSetDoAfterEvent(),
                ent.Owner,
                target: ent.Owner,
                used: args.User)
            {
                BreakOnMove = true,
                AttemptFrequency = AttemptFrequency.StartAndEnd,
            };

            verb.Text = Loc.GetString("trap-component-set-trap");
            verb.Act = () =>
            {
                if (CanArmTrap(ent.Owner, localUser))
                    _doAfter.TryStartDoAfter(setDoAfter);
            };
        }
        args.Verbs.Add(verb);
    }

    private void OnSetDoAfter(Entity<TrapComponent> ent, ref TrapSetDoAfterEvent args)
    {
        if (args.Cancelled || !CanArmTrap(ent.Owner, args.User))
            return;

        StartTrapToggle(ent, args.User);
    }

    private void OnDefuseDoAfter(Entity<TrapComponent> ent, ref TrapDefuseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        StartTrapToggle(ent, args.User);
    }

    private void OnStepTriggerAttempt(Entity<TrapComponent> ent, ref StartCollideEvent args)
    {
        if(!ent.Comp.IsArmed)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        ToggleTrap(ent.Owner, ent.Comp);
        _trigger.TriggerTarget(ent.Owner, args.OtherEntity);

        if(_net.IsServer)
            _audio.PlayPvs(ent.Comp.HitTrapSound, ent.Owner);
    }

    private void OnTrigger(Entity<TrapComponent> ent, ref SharedTriggerEvent args)
    {
        if (!args.Activator.HasValue)
            return;

        if (!TryComp<EnsnaringComponent>(ent.Owner, out var ensnaring))
            return;

        if (ent.Comp.DurationStun != TimeSpan.Zero && TryComp<StatusEffectsComponent>(args.Activator.Value, out var status))
        {
            _stunSystem.TryStun(args.Activator.Value, ent.Comp.DurationStun, true, status);
            _stunSystem.TryKnockdown(args.Activator.Value, ent.Comp.DurationStun, true, status);
        }

        _ensnareableSystem.TryEnsnare(args.Activator.Value, ent.Owner, ensnaring);
    }

    private void UpdateVisuals(EntityUid uid,TrapComponent? trapComp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref trapComp, ref appearance, false))
            return;

        _appearance.SetData(uid, TrapVisuals.Visual,
            trapComp.IsArmed ? TrapVisuals.Armed : TrapVisuals.Unarmed, appearance);
    }

    private void StartTrapToggle(Entity<TrapComponent> ent, EntityUid user)
    {
        var xform = Transform(ent.Owner).Coordinates;
        _audio.PlayPredicted(ent.Comp.IsArmed ? ent.Comp.SetTrapSound : ent.Comp.DefuseTrapSound, xform, user);
        ToggleTrap(ent.Owner, ent.Comp);
    }

    private void ToggleTrap(EntityUid uid, TrapComponent comp)
    {
        comp.IsArmed = !comp.IsArmed;
        Dirty(uid,comp);
        UpdateVisuals(uid);

        if (comp.IsArmed)
            _transformSystem.AnchorEntity(uid);
        else
            _transformSystem.Unanchor(uid);

        var ev = new TrapToggledEvent(comp.IsArmed);
        RaiseLocalEvent(uid, ev);
    }

    private bool CanArmTrap(EntityUid trapEntity, EntityUid user)
    {
        //Providing a stuck traps on one tile
        var coordinates = Transform(trapEntity).Coordinates;
        if (_anchorableSystem.AnyUnstackable(trapEntity, coordinates) || _transformSystem.GetGrid(coordinates) == null)
        {
            _popup.PopupClient(Loc.GetString("trap-component-no-room"), user, user);
            return false;
        }
        return true;
    }
}
