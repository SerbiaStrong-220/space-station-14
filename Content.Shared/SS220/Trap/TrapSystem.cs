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
        SubscribeLocalEvent<TrapComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<TrapComponent, TrapInteractionDoAfterEvent>(OnTrapInteractionDoAfter);
        SubscribeLocalEvent<TrapComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<TrapComponent, SharedTriggerEvent>(OnTrigger);
    }

    private void OnAlternativeVerb(Entity<TrapComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (_openable.IsClosed(args.Target))
            return;

        var doAfterEv = new TrapInteractionDoAfterEvent();
        var verb = new AlternativeVerb();
        var user = args.User;

        if (ent.Comp.State == TrapArmedState.Armed)
        {
            if (!CanDefuseTrap(ent, user))
                return;

            verb.Text = Loc.GetString("trap-component-defuse-trap");
            doAfterEv.ArmAction = false;
        }
        else
        {
            if (!CanArmTrap(ent, user))
                return;

            verb.Text = Loc.GetString("trap-component-set-trap");
            doAfterEv.ArmAction = true;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.State == TrapArmedState.Armed ? ent.Comp.DefuseTrapDelay : ent.Comp.SetTrapDelay,
            doAfterEv,
            ent.Owner,
            target: ent.Owner,
            used: args.User)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        verb.Act = () => _doAfter.TryStartDoAfter(doAfter);
        args.Verbs.Add(verb);
    }

    private void OnTrapInteractionDoAfter(Entity<TrapComponent> ent, ref TrapInteractionDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.ArmAction)
            ArmTrap(ent, args.User);
        else
            DefuseTrap(ent, args.User);
    }

    public void ArmTrap(Entity<TrapComponent> ent, EntityUid? user, bool withSound = true)
    {
        if (!CanArmTrap(ent, user))
            return;
        var xform = Transform(ent.Owner).Coordinates;
        if (user != null && withSound)
            _audio.PlayPredicted(ent.Comp.SetTrapSound, xform, user);

        ent.Comp.State =TrapArmedState.Armed;
        Dirty(ent);
        UpdateVisuals(ent.Owner, ent.Comp);
        _transformSystem.AnchorEntity(ent.Owner);

        var ev = new TrapArmedEvent();
        RaiseLocalEvent(ent, ev);
    }

    public void DefuseTrap(Entity<TrapComponent> ent, EntityUid? user, bool withSound = true)
    {
        if (!CanDefuseTrap(ent, user))
            return;

        var xform = Transform(ent.Owner).Coordinates;
        if (user != null && withSound)
            _audio.PlayPredicted(ent.Comp.DefuseTrapSound, xform, user);

        ent.Comp.State = TrapArmedState.Unarmed;
        Dirty(ent);
        UpdateVisuals(ent.Owner, ent.Comp);
        _transformSystem.Unanchor(ent.Owner);

        var ev = new TrapDefusedEvent();
        RaiseLocalEvent(ent, ev);
    }

    public bool CanArmTrap(Entity<TrapComponent> ent, EntityUid? user)
    {
        // Providing a stuck traps on one tile
        var coordinates = Transform(ent.Owner).Coordinates;
        if (_anchorableSystem.AnyUnstackable(ent.Owner, coordinates) || _transformSystem.GetGrid(coordinates) == null)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("trap-component-no-room"), user.Value, user.Value);
            return false;
        }

        var ev = new TrapArmAttemptEvent(user);
        RaiseLocalEvent(ent, ev);

        return !ev.Cancelled;
    }

    public bool CanDefuseTrap(Entity<TrapComponent> ent, EntityUid? user)
    {
        var ev = new TrapDefuseAttemptEvent(user);
        RaiseLocalEvent(ent, ev);

        return !ev.Cancelled;
    }

    private void OnStartCollide(Entity<TrapComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.State == TrapArmedState.Unarmed)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        DefuseTrap(ent, args.OtherEntity, false);
        _trigger.TriggerTarget(ent.Owner, args.OtherEntity);

        if (_net.IsServer)
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

    private void UpdateVisuals(EntityUid uid, TrapComponent? trapComp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref trapComp, ref appearance, false))
            return;

        _appearance.SetData(uid, TrapVisuals.Visual,
            trapComp.State == TrapArmedState.Unarmed ? TrapVisuals.Unarmed : TrapVisuals.Armed, appearance);
    }
}

