// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Construction.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.Components;
using Content.Shared.Trigger;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// The logic of traps witch look like bears trap. Automatically “binds to leg” when activated.
/// </summary>
public sealed class TrapSystem : EntitySystem
{
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
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TrapComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<TrapComponent, TrapInteractionDoAfterEvent>(OnTrapInteractionDoAfter);
        SubscribeLocalEvent<TrapComponent, TriggerEvent>(OnTrigger);
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

    private void ArmTrap(Entity<TrapComponent> ent, EntityUid? user, bool withSound = true)
    {
        if (!CanArmTrap(ent, user))
            return;

        var coordinates = Transform(ent.Owner).Coordinates;

        if (user != null && withSound)
            _audio.PlayPredicted(ent.Comp.SetTrapSound, coordinates, user);

        ent.Comp.State = TrapArmedState.Armed;
        Dirty(ent);

        UpdateVisuals(ent.Owner, ent.Comp);
        _transformSystem.AnchorEntity(ent.Owner);

        var ev = new TrapArmedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void DefuseTrap(Entity<TrapComponent> ent, EntityUid? user, bool withSound = true)
    {
        if (!CanDefuseTrap(ent, user))
            return;

        var coordinates = Transform(ent.Owner).Coordinates;

        if (user != null && withSound)
            _audio.PlayPredicted(ent.Comp.DefuseTrapSound, coordinates, user);

        ent.Comp.State = TrapArmedState.Unarmed;
        Dirty(ent);

        UpdateVisuals(ent.Owner, ent.Comp);
        _transformSystem.Unanchor(ent.Owner);

        var ev = new TrapDefusedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private bool CanArmTrap(Entity<TrapComponent> ent, EntityUid? user)
    {
        // Providing a stuck traps on one tile
        var coordinates = Transform(ent.Owner).Coordinates;
        if (_anchorableSystem.AnyUnstackable(ent.Owner, coordinates) || _transformSystem.GetGrid(coordinates) == null)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("trap-component-no-room"), user.Value, user.Value);

            return false;
        }

        // arming in container cause crashes
        if (_container.IsEntityInContainer(ent))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("trap-component-in-container"), user.Value, user.Value);

            return false;
        }

        var ev = new TrapArmAttemptEvent(user);
        RaiseLocalEvent(ent, ref ev);

        return !ev.Cancelled;
    }

    private bool CanDefuseTrap(Entity<TrapComponent> ent, EntityUid? user)
    {
        // disarming in container can cause crash
        if (_container.IsEntityInContainer(ent))
            return false;

        var ev = new TrapDefuseAttemptEvent(user);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Cancelled;
    }

    private void OnTrigger(Entity<TrapComponent> ent, ref TriggerEvent args)
    {
        if (ent.Comp.State == TrapArmedState.Unarmed)
            return;

        if (args.User == null)
            return;

        if (!TryComp<EnsnaringComponent>(ent.Owner, out var ensnaring))
            return;

        DefuseTrap(ent, args.User.Value, false);

        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.HitTrapSound, ent.Owner);

        if (ent.Comp.DurationStun != TimeSpan.Zero && TryComp<StatusEffectsComponent>(args.User.Value, out _))
        {
            _stunSystem.TryUpdateStunDuration(args.User.Value, ent.Comp.DurationStun);
            _stunSystem.TryKnockdown(args.User.Value, ent.Comp.DurationStun);
        }

        _ensnareableSystem.TryEnsnare(args.User.Value, ent.Owner, ensnaring);
        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(args.User.Value)} caused trap {ToPrettyString(ent.Owner):entity}");
    }

    private void UpdateVisuals(EntityUid uid, TrapComponent? trapComp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref trapComp, ref appearance, false))
            return;

        _appearance.SetData(uid,
            TrapVisuals.Visual,
            trapComp.State == TrapArmedState.Unarmed ? TrapVisuals.Unarmed : TrapVisuals.Armed,
            appearance);
    }
}

