// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Input;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed partial class GunUseSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(KeyFunctions220.UseGunInHand, InputCmdHandler.FromDelegate(HandleUseGun, handle: false, outsidePrediction: false))
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

        var ev = new GunCycleRequestEvent(uid, held.Value);
        RaiseLocalEvent(held.Value, ref ev);

        return true;
    }
}
