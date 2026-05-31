// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Weapons.Ranged.Systems;
public sealed class GunUseSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.UseGunInHand, InputCmdHandler.FromDelegate(HandleUseGun, handle: false, outsidePrediction: false))
            .Register<GunUseSystem>();
    }

    public void HandleUseGun(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryUseGunInHand(session.AttachedEntity.Value);
    }
    public bool TryUseGunInHand(EntityUid uid, bool altInteract = false, HandsComponent? handsComp = null, string? handName = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        if (!Resolve(uid, ref handsComp, false))
            return false;

        var hand = handName;

        if (!_hands.TryGetHand(uid, hand, out _))
            hand = handsComp.ActiveHandId;

        if (!_hands.TryGetHeldItem((uid, handsComp), hand, out var held))
            return false;

        if (!HasComp<GunComponent>(held.Value))
            return false;

        if (!TryGetNetEntity(uid, out var netUser) || !TryGetNetEntity(held.Value, out var netGun))
            return false;

        var useMsg = new GunCycleRequestEvent((NetEntity)netUser, (NetEntity)netGun);
        RaisePredictiveEvent(useMsg);

        return true;
    }
}
