using System.Numerics;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinid;

/// <summary>
/// Throws a ground-recoil shooter <c>GunComponent.GroundRecoil</c> tiles away from the shot and may knock them down.
/// Weightless shooters keep the regular impulse push.
/// </summary>
public sealed partial class GunRecoilModifierSystem : EntitySystem
{
    private const float RecoilThrowSpeed = 2f;

    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunRecoilModifierComponent, ShooterImpulseEvent>(OnShooterImpulse);
        SubscribeLocalEvent<GroundRecoilKnockdownModifierComponent,
            InventoryRelayedEvent<GetGroundRecoilKnockdownModifierEvent>>(OnGetKnockdownModifier);
    }

    private void OnShooterImpulse(Entity<GunRecoilModifierComponent> ent, ref ShooterImpulseEvent args)
    {
        var gun = args.Gun.Comp;
        var shot = _transform.ToMapCoordinates(args.To).Position - _transform.ToMapCoordinates(args.From).Position;

        // Already flying from the previous shot: an automatic burst would otherwise stack impulses
        // while friction and movement are suspended mid-air.
        if (gun.GroundRecoil <= 0f ||
            shot == Vector2.Zero ||
            _gravity.IsWeightless(ent.Owner) ||
            HasComp<ThrownItemComponent>(ent.Owner))
        {
            return;
        }

        // A slow throw keeps the shooter airborne for nearly the whole distance, so floor friction barely changes it.
        _throwing.TryThrow(
            ent.Owner,
            -shot.Normalized() * gun.GroundRecoil,
            RecoilThrowSpeed,
            recoil: false,
            animated: false,
            playSound: false,
            doSpin: false);

        var modifier = new GetGroundRecoilKnockdownModifierEvent();
        RaiseLocalEvent(ent.Owner, ref modifier);
        if (TryComp<InventoryComponent>(ent.Owner, out var inventory))
            _inventory.RelayEvent((ent.Owner, inventory), ref modifier);

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        if (new System.Random(seed).Prob(gun.GroundRecoilKnockdownChance * modifier.Multiplier))
            _stun.TryKnockdown(ent.Owner, gun.GroundRecoilKnockdownTime, drop: true);
    }

    private void OnGetKnockdownModifier(
        Entity<GroundRecoilKnockdownModifierComponent> ent,
        ref InventoryRelayedEvent<GetGroundRecoilKnockdownModifierEvent> args)
    {
        args.Args.Multiplier *= ent.Comp.Multiplier;
    }
}
