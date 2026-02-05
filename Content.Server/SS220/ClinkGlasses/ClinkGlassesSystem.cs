// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Content.Shared.SS220.ClinkGlasses;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.ClinkGlasses;

public sealed class ClinkGlassesSystem : SharedClinkGlassesSystem
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
            ("receiver", receiver.Owner),
            ("item", receiver.Comp.Item),
            ("initiator", receiver.Comp.Initiator));

        _popupSystem.PopupEntity(loc, receiver.Owner, PopupType.Medium);

        _audio.PlayPvs(_soundOnComplete, receiver.Owner);

        _alerts.ClearAlert(receiver.Owner, _clinkGlassesAlert);

        _entManager.RemoveComponent<ClinkGlassesReceiverComponent>(receiver);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ClinkGlassesReceiverComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out _))
        {
            var receiverPos = Transform(comp.Initiator).Coordinates;
            var giverPos = Transform(uid).Coordinates;
            receiverPos.TryDistance(EntityManager, giverPos, out var distance);

            if (distance < comp.ReceiveRange)
                continue;

            if (distance > comp.ReceiveRange)
            {
                _alerts.ClearAlert(uid, _clinkGlassesAlert);
                _entManager.RemoveComponent<ClinkGlassesReceiverComponent>(uid);
            }
        }
    }

    protected override void DoClinkGlassesOffer(EntityUid user, EntityUid target)
    {
        if (!_hands.TryGetActiveItem(user, out var item) && !HasComp<ClinkGlassesComponent>(item))
            return;

        var itemReceiver = EnsureComp<ClinkGlassesReceiverComponent>(target);
        itemReceiver.Initiator = user;
        itemReceiver.Item = (EntityUid)item;
        _alerts.ShowAlert(target, _clinkGlassesAlert);

        var loc = Loc.GetString("loc-clink-glasses-attempt",
            ("initiator", user),
            ("item", item));

        _popupSystem.PopupEntity(loc, user, PopupType.Medium);
    }
}
