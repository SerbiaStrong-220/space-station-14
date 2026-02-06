// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Content.Shared.SS220.ClinkGlasses;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Numerics;
using Content.Shared.Weapons.Melee;
using Content.Shared.IdentityManagement;

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

    private readonly ProtoId<AlertPrototype> _clinkGlassesAlert = "ClinkGlasses";

    private readonly SoundSpecifier _soundOnComplete = new SoundPathSpecifier("/Audio/SS220/Effects/clink_glasses.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.5f)
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClinkGlassesReceiverComponent, ClinkGlassesAlertEvent>(OnClinkGlassesAlertClicked);
    }

    private void OnClinkGlassesAlertClicked(Entity<ClinkGlassesReceiverComponent> receiver, ref ClinkGlassesAlertEvent args)
    {
        var loc = Loc.GetString("loc-clink-glasses-success",
            ("receiver", Identity.Name(receiver.Owner, EntityManager)),
            ("item", receiver.Comp.Item),
            ("initiator", Identity.Name(receiver.Comp.Initiator, EntityManager)));

        _popupSystem.PopupEntity(loc, receiver.Owner, PopupType.Medium);

        _audio.PlayPvs(_soundOnComplete, receiver.Owner);

        _alerts.ClearAlert(receiver.Owner, _clinkGlassesAlert);

        _entManager.RemoveComponent<ClinkGlassesReceiverComponent>(receiver);

        var xform = Transform(receiver.Owner);

        var initiatorPos = _transformSystem.GetWorldPosition(receiver.Comp.Initiator);

        var localPos = Vector2.Transform(initiatorPos, _transformSystem.GetInvWorldMatrix(xform));
        localPos = xform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(receiver.Owner, receiver.Owner, Angle.Zero, localPos, null, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ClinkGlassesReceiverComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out _))
        {
            var initiatorCoords = Transform(comp.Initiator).Coordinates;
            var receiverCoords = Transform(uid).Coordinates;

            if (!initiatorCoords.TryDistance(EntityManager, receiverCoords, out var distance) || distance > comp.ReceiveRange)
            {
                _alerts.ClearAlert(uid, _clinkGlassesAlert);
                _entManager.RemoveComponent<ClinkGlassesReceiverComponent>(uid);
            }
        }
    }

    protected override void DoClinkGlassesOffer(EntityUid user, EntityUid target)
    {
        if (!_hands.TryGetActiveItem(user, out var item) || !HasComp<ClinkGlassesComponent>(item))
            return;

        var itemReceiver = EnsureComp<ClinkGlassesReceiverComponent>(target);
        itemReceiver.Initiator = user;
        itemReceiver.Item = (EntityUid)item;
        _alerts.ShowAlert(target, _clinkGlassesAlert);

        var loc = Loc.GetString("loc-clink-glasses-attempt",
            ("initiator", Identity.Name(user, EntityManager)),
            ("item", item));

        _popupSystem.PopupEntity(loc, user, PopupType.Medium);
    }
}
