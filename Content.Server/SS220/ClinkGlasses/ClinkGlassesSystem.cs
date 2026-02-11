// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Content.Shared.SS220.ClinkGlasses;
using Content.Shared.Weapons.Melee;
using Content.Shared.IdentityManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.SS220.ClinkGlasses;

public sealed class ClinkGlassesSystem : SharedClinkGlassesSystem
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly ProtoId<AlertPrototype> _clinkGlassesAlert = "ClinkGlasses";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClinkGlassesReceiverComponent, ClinkGlassesAlertEvent>(OnClinkGlassesAlertClicked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ClinkGlassesReceiverComponent>();
        while (enumerator.MoveNext(out var uid, out var clinkGlassesComp))
        {
            // Check passed time
            clinkGlassesComp.LifeTime -= frameTime;
            if (float.IsNegative(clinkGlassesComp.LifeTime))
            {
                EndAction(uid);
                continue;
            }

            // Check validity
            if (!HasComp<ClinkGlassesInitiatorComponent>(uid) || !HasComp<ClinkGlassesInitiatorComponent>(clinkGlassesComp.Initiator))
            {
                EndAction(uid);
                continue;
            }

            // Check distance
            var initiatorCoords = Transform(clinkGlassesComp.Initiator).Coordinates;
            var receiverCoords = Transform(uid).Coordinates;
            if (!initiatorCoords.TryDistance(EntityManager, receiverCoords, out var distance) || distance > clinkGlassesComp.ReceiveRange)
                EndAction(uid);
        }
    }

    private void OnClinkGlassesAlertClicked(Entity<ClinkGlassesReceiverComponent> receiver, ref ClinkGlassesAlertEvent args)
    {
        if (!_hands.TryGetActiveItem(receiver.Owner, out var itemInHand) || !HasComp<ClinkGlassesComponent>(itemInHand))
            return;

        if (receiver.Comp.Initiator != receiver.Owner)
            DoClinkGlass(receiver.Owner, receiver.Comp.Initiator, itemInHand.Value);

        EndAction(receiver.Owner);
    }


    protected override void DoClinkGlassesOffer(EntityUid initiator, EntityUid receiver, EntityUid item)
    {
        if (!TryComp<ClinkGlassesInitiatorComponent>(initiator, out var initiatorComp))
            return;

        initiatorComp.NextClinkTime = _gameTiming.CurTime + initiatorComp.Cooldown;

        if (TryComp<ClinkGlassesReceiverComponent>(initiator, out var receiverCompOnInitiator) && receiverCompOnInitiator.Initiator == receiver)
        {
            // Initiator already have offer from receiver. Clink glasses and remove comps from both
            DoClinkGlass(initiator, receiverCompOnInitiator.Initiator, item);
            EndAction(initiator);
            EndAction(receiver);
            return;
        }

        if (TryComp<ClinkGlassesReceiverComponent>(receiver, out var receiverCompOnReceiver) && receiverCompOnReceiver.Initiator == receiver)
        {
            // Receiver raised glass for everyone. Just clink glasses
            DoClinkGlass(initiator, receiver, item);
            return;
        }

        MarkEntityForAction(initiator, receiver);

        var loc = Loc.GetString("clink-glasses-attempt",
            ("initiator", Identity.Name(initiator, EntityManager)),
            ("item", item));

        _popupSystem.PopupEntity(loc, initiator);
    }

    protected override void DoRaiseGlass(EntityUid initiator, EntityUid item)
    {
        MarkEntityForAction(initiator, initiator);

        var loc = Loc.GetString("clink-glasses-raised",
            ("initiator", Identity.Name(initiator, EntityManager)),
            ("item", item));

        _popupSystem.PopupEntity(loc, initiator, PopupType.Medium);
    }

    private void DoClinkGlass(EntityUid receiver, EntityUid initiator, EntityUid item)
    {
        var loc = Loc.GetString("clink-glasses-success",
            ("receiver", Identity.Name(receiver, EntityManager)),
            ("item", item),
            ("initiator", Identity.Name(initiator, EntityManager)));

        _popupSystem.PopupEntity(loc, receiver);

        if (TryComp<ClinkGlassesComponent>(item, out var comp))
            _audio.PlayPvs(comp.SoundOnComplete, receiver);

        // Animation
        var xform = Transform(receiver);
        var initiatorPos = _transformSystem.GetWorldPosition(initiator);
        var localPos = Vector2.Transform(initiatorPos, _transformSystem.GetInvWorldMatrix(xform));
        localPos = xform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(receiver, receiver, Angle.Zero, localPos, null, false);
    }

    private void MarkEntityForAction(EntityUid initiator, EntityUid receiver)
    {
        if (!TryComp<ClinkGlassesInitiatorComponent>(initiator, out var initiatorComp))
            return;

        initiatorComp.NextClinkTime = _gameTiming.CurTime + initiatorComp.Cooldown;

        var receiverComp = EnsureComp<ClinkGlassesReceiverComponent>(receiver);
        receiverComp.Initiator = initiator;
        receiverComp.LifeTime = ClinkGlassesReceiverComponent.BaseLifeTime;
        _alerts.ShowAlert(receiver, _clinkGlassesAlert);
    }

    private void EndAction(EntityUid uid)
    {
        _alerts.ClearAlert(uid, _clinkGlassesAlert);
        _entManager.RemoveComponent<ClinkGlassesReceiverComponent>(uid);
    }
}
