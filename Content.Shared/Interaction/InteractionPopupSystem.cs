using Content.Shared.Bed.Sleep;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<InteractionPopupComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(EntityUid uid, InteractionPopupComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!component.OnActivate)
            return;

        SharedInteract(uid, component, args, args.Target, args.User);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        SharedInteract(uid, component, args, args.Target, args.User);
    }

    private void SharedInteract(
        EntityUid uid,
        InteractionPopupComponent component,
        HandledEntityEventArgs args,
        EntityUid target,
        EntityUid user)
    {
        if (args.Handled || user == target)
            return;

        //Handling does nothing and this thing annoyingly plays way too often.
        // HUH? What does this comment even mean?

        if (HasComp<SleepingComponent>(uid))
            return;

        if (TryComp<MobStateComponent>(uid, out var state)
            && !_mobStateSystem.IsAlive(uid, state))
        {
            return;
        }

        //ss220 interact w/o hands fix start
        if (component.NeedHands && !HasComp<HandsComponent>(user))
            return;
        //ss220 interact w/o hands fix end

        args.Handled = true;

        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return;

        component.LastInteractTime = curTime;

        // TODO: Should be an attempt event
        // TODO: Need to handle pausing with an accumulator.

        var msg = ""; // Stores the text to be shown in the popup message
        SoundSpecifier? sfx = null; // Stores the filepath of the sound to be played
        string? perceivedByOthers; //SS220-NonTactile

        var predict = component.SuccessChance is 0 or 1
                      && component.InteractSuccessSpawn == null
                      && component.InteractFailureSpawn == null;

        if (_netMan.IsClient && !predict)
            return;

        if (_random.Prob(component.SuccessChance))
        {
            //SS220-NonTactile begin
            var ev = new InteractionSuccessEvent(user)
            {
                Message = component.InteractSuccessString,
                Sound = component.InteractSuccessSound,
                MessagePerceivedByOthers = component.MessagePerceivedByOthers,
            };
            RaiseLocalEvent(target, ref ev);

            if (ev.Message != null)
                msg = Loc.GetString(ev.Message, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            sfx = ev.Sound;
            perceivedByOthers = ev.MessagePerceivedByOthers;
            //SS220-NonTactile end

            if (component.InteractSuccessSpawn != null)
                Spawn(component.InteractSuccessSpawn, _transform.GetMapCoordinates(uid));
        }
        else
        {
            //SS220-NonTactile begin
            var ev = new InteractionFailureEvent(user)
            {
                Message = component.InteractFailureString,
                Sound = component.InteractFailureSound,
                MessagePerceivedByOthers = component.MessagePerceivedByOthers,
            };
            RaiseLocalEvent(target, ref ev);

            if (ev.Message != null)
                msg = Loc.GetString(ev.Message, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            sfx = ev.Sound;
            perceivedByOthers = ev.MessagePerceivedByOthers;
            //SS220-NonTactile end

            if (component.InteractFailureSpawn != null)
                Spawn(component.InteractFailureSpawn, _transform.GetMapCoordinates(uid));
        }

        if (!string.IsNullOrEmpty(perceivedByOthers)) //SS220-NonTactile
        {
            var msgOthers = Loc.GetString(perceivedByOthers, //SS220-NonTactile
                ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msgOthers, uid, Filter.PvsExcept(user, entityManager: EntityManager), true);
        }

        if (!predict)
        {
            _popupSystem.PopupEntity(msg, uid, user);

            if (component.SoundPerceivedByOthers)
                _audio.PlayPvs(sfx, target);
            else
                _audio.PlayEntity(sfx, Filter.Entities(user, target), target, false);
            return;
        }

        _popupSystem.PopupClient(msg, uid, user);

        if (sfx == null)
            return;

        if (component.SoundPerceivedByOthers)
        {
            _audio.PlayPredicted(sfx, target, user);
            return;
        }

        if (_netMan.IsClient)
        {
            if (_gameTiming.IsFirstTimePredicted)
                _audio.PlayEntity(sfx, Filter.Local(), target, true);
        }
        else
        {
            _audio.PlayEntity(sfx, Filter.Empty().FromEntities(target), target, false);
        }
    }

    /// <summary>
    /// Sets <see cref="InteractionPopupComponent.InteractSuccessString"/>.
    /// </summary>
    /// <para>
    /// This field is not networked automatically, so this method must be called on both sides of the network.
    /// </para>
    public void SetInteractSuccessString(Entity<InteractionPopupComponent> ent, string str)
    {
        ent.Comp.InteractSuccessString = str;
    }

    /// <summary>
    /// Sets <see cref="InteractionPopupComponent.InteractFailureString"/>.
    /// </summary>
    /// <para>
    /// This field is not networked automatically, so this method must be called on both sides of the network.
    /// </para>
    public void SetInteractFailureString(Entity<InteractionPopupComponent> ent, string str)
    {
        ent.Comp.InteractFailureString = str;
    }
}
