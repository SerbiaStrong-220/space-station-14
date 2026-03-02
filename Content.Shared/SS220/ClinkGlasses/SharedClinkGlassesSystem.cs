// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ClinkGlasses;

public sealed class SharedClinkGlassesSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/glass-celebration.png"));
    private static readonly ProtoId<AlertPrototype> ClinkGlassesAlert = "ClinkGlasses";

    public override void Initialize()
    {
        SubscribeLocalEvent<ClinkGlassesComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<ClinkGlassesComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<ClinkGlassesComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);

        SubscribeLocalEvent<ClinkGlassesInitiatorComponent, GetVerbsEvent<AlternativeVerb>>(OnInitiatorAlternativeVerb);

        SubscribeLocalEvent<ClinkGlassesReceiverComponent, ClinkGlassesAlertEvent>(OnClinkGlassesAlertClicked);
    }

    public override void Update(float frameTime)
    {
        var enumerator = EntityQueryEnumerator<ClinkGlassesReceiverComponent>();
        while (enumerator.MoveNext(out var uid, out var clinkGlassesComp))
        {
            // Check passed time
            if (_gameTiming.CurTime > clinkGlassesComp.LifeTime)
            {
                EndClinkGlassesReceiver(uid);
                continue;
            }

            // Check validity
            if (!HasComp<ClinkGlassesInitiatorComponent>(uid) || !HasComp<ClinkGlassesInitiatorComponent>(clinkGlassesComp.Initiator))
            {
                EndClinkGlassesReceiver(uid);
                continue;
            }

            // Check distance
            if (!CheckDistance(uid, clinkGlassesComp.Initiator))
                EndClinkGlassesReceiver(uid);
        }
    }

    #region Local Events Handlers

    private void OnVerb(Entity<ClinkGlassesComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<ClinkGlassesInitiatorComponent>(args.User))
            return;

        var user = args.User;
        var verb = new Verb
        {
            Text = Loc.GetString("raise-glass-verb-text"),
            Act = () =>
            {
                DoRaiseGlass(user, ent.Owner);
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    private void OnGotEquippedHand(Entity<ClinkGlassesComponent> ent, ref GotEquippedHandEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;

        EnsureComp<ClinkGlassesInitiatorComponent>(args.User, out var comp);
        comp.Items.Add(ent);
        Dirty(args.User, comp);
    }

    private void OnGotUnequippedHand(Entity<ClinkGlassesComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;

        if (!TryComp<ClinkGlassesInitiatorComponent>(args.User, out var comp))
            return;

        comp.Items.Remove(ent);

        if (comp.Items.Count == 0)
            RemComp<ClinkGlassesInitiatorComponent>(args.User);

        DirtyEntity(ent);
    }

    private void OnInitiatorAlternativeVerb(Entity<ClinkGlassesInitiatorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<ClinkGlassesInitiatorComponent>(args.User))
            return;

        if (!CheckDistance(args.User, args.Target))
            return;

        if (args.User == args.Target)
            return;

        if (!_hands.TryGetActiveItem(args.User, out var itemInHand) || !HasComp<ClinkGlassesComponent>(itemInHand))
            return;

        var initiator = args.User;
        var receiver = args.Target;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("clink-glasses-verb-text"),
            Act = () =>
            {
                DoClinkGlassesOffer(initiator, receiver, itemInHand.Value);
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    private void OnClinkGlassesAlertClicked(Entity<ClinkGlassesReceiverComponent> receiver, ref ClinkGlassesAlertEvent args)
    {
        if (_hands.TryGetActiveItem(receiver.Owner, out var itemInHand)
            && HasComp<ClinkGlassesComponent>(itemInHand)
            && receiver.Comp.Initiator != receiver.Owner)
        {
            DoClinkGlass(receiver.Owner, receiver.Comp.Initiator, itemInHand.Value);
        }

        EndClinkGlassesReceiver(receiver.Owner);
    }

    #endregion

    #region Main functions

    private void DoRaiseGlass(EntityUid initiator, EntityUid item)
    {
        MakeEntityClinkGlassReceiver(initiator, initiator);

        if (!UseCooldown(initiator)) // Prevents popup spam
            return;

        var locInitiator = Loc.GetString("clink-glasses-raised-self",
            ("item", item));

        var locOthers = Loc.GetString("clink-glasses-raised-others",
            ("initiator", Identity.Name(initiator, EntityManager)),
            ("item", item));

        _popupSystem.PopupPredicted(locInitiator, locOthers, initiator, initiator, PopupType.Medium);
    }

    private void DoClinkGlassesOffer(EntityUid initiator, EntityUid receiver, EntityUid item)
    {
        // If initiator already have offer from receiver - clink glasses
        if (TryComp<ClinkGlassesReceiverComponent>(initiator, out var receiverCompOnInitiator) && receiverCompOnInitiator.Initiator == receiver)
        {
            DoClinkGlass(initiator, receiverCompOnInitiator.Initiator, item);
            EndClinkGlassesReceiver(initiator);
            return;
        }

        // If receiver raised glass for everyone - just clink glasses
        if (TryComp<ClinkGlassesReceiverComponent>(receiver, out var receiverCompOnReceiver) && receiverCompOnReceiver.Initiator == receiver)
        {
            if (!UseCooldown(initiator)) // Prevents clinking spam
                return;

            DoClinkGlass(initiator, receiver, item);
            return;
        }

        if (!UseCooldown(initiator)) // Prevents overall spam
            return;

        MakeEntityClinkGlassReceiver(initiator, receiver);

        var locInitiator = Loc.GetString("clink-glasses-attempt-self",
            ("item", item));

        var locOthers = Loc.GetString("clink-glasses-attempt-others",
            ("initiator", Identity.Name(initiator, EntityManager)),
            ("item", item));

        _popupSystem.PopupPredicted(locInitiator, locOthers, initiator, initiator);
    }

    private void DoClinkGlass(EntityUid receiver, EntityUid initiator, EntityUid item)
    {
        var locReceiver = Loc.GetString("clink-glasses-success-self",
            ("item", item),
            ("initiator", Identity.Name(initiator, EntityManager)));

        var locOthers = Loc.GetString("clink-glasses-success-others",
            ("receiver", Identity.Name(receiver, EntityManager)),
            ("item", item),
            ("initiator", Identity.Name(initiator, EntityManager)));

        _popupSystem.PopupPredicted(locReceiver, locOthers, receiver, receiver);

        if (TryComp<ClinkGlassesComponent>(item, out var comp))
            _audio.PlayPredicted(comp.SoundOnComplete, receiver, receiver);

        // Animation
        var xform = Transform(receiver);
        var initiatorPos = _transformSystem.GetWorldPosition(initiator);
        var localPos = Vector2.Transform(initiatorPos, _transformSystem.GetInvWorldMatrix(xform));
        localPos = xform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(receiver, receiver, Angle.Zero, localPos, null, true);
    }

    private void MakeEntityClinkGlassReceiver(EntityUid initiator, EntityUid receiver)
    {
        EnsureComp<ClinkGlassesReceiverComponent>(receiver, out var receiverComp);
        receiverComp.Initiator = initiator;
        receiverComp.LifeTime = _gameTiming.CurTime + TimeSpan.FromSeconds(receiverComp.BaseLifeTime);
        _alerts.ShowAlert(receiver, ClinkGlassesAlert);
        Dirty(receiver, receiverComp);
    }

    #endregion

    #region Helper functions

    /// <summary>
    ///     Returns if distance between two entities valid.
    /// </summary>
    private bool CheckDistance(EntityUid initiator, EntityUid receiver)
    {
        var initiatorCoords = Transform(initiator).Coordinates;
        var receiverCoords = Transform(receiver).Coordinates;
        if (initiatorCoords.TryDistance(EntityManager, receiverCoords, out var distance) && distance < ClinkGlassesReceiverComponent.ReceiveRange)
            return true;

        return false;
    }

    /// <summary>
    ///     Returns if cooldown elapsed. If true - applies it.
    /// </summary>
    private bool UseCooldown(EntityUid initiator)
    {
        if (!TryComp<ClinkGlassesInitiatorComponent>(initiator, out var initiatorComp))
            return false;

        if (_gameTiming.CurTime < initiatorComp.NextClinkTime)
            return false;

        initiatorComp.NextClinkTime = _gameTiming.CurTime + TimeSpan.FromSeconds(initiatorComp.Cooldown);
        Dirty(initiator, initiatorComp);

        return true;
    }

    /// <summary>
    ///     Removes ClinkGlassesReceiverComponent from EntityUid and clears alert.
    /// </summary>
    private void EndClinkGlassesReceiver(EntityUid uid)
    {
        RemComp<ClinkGlassesReceiverComponent>(uid);
        _alerts.ClearAlert(uid, ClinkGlassesAlert);
    }

    #endregion
}
