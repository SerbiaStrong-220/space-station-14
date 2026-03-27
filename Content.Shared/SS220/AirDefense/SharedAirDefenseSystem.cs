using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.PowerCell.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AirDefense;

public sealed partial class SharedAirDefenseSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly EntProtoId EffectEmpPulse = "EffectEmpPulse";
    private const string BulletFixture = "fly-by";

    public override void Initialize()
    {
        SubscribeLocalEvent<AirDefenseComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<AirDefenseComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnPowerCellChanged(Entity<AirDefenseComponent> ent, ref PowerCellChangedEvent args)
    {
        _appearance.SetData(ent, AirDefenseCellVisuals.HasPowerCell, !args.Ejected);
    }

    private void OnStartCollide(Entity<AirDefenseComponent> ent, ref StartCollideEvent args)
    {
        if (args.OtherFixtureId != BulletFixture)
            return;

        if (!_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.OtherEntity))
            return;

        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        if (!TryGetRadius(fixtures, out var radius))
            return;

        var targetCoords = Transform(args.OtherEntity).Coordinates;
        if (!_interaction.InRangeUnobstructed(ent, targetCoords, radius, CollisionGroup.Opaque))
            return;

        ExecuteDefensiveFire(ent, args.OtherEntity);
    }

    private static bool TryGetRadius(FixturesComponent fixtures, out float radius)
    {
        radius = 0;
        if (!fixtures.Fixtures.TryGetValue("airDefense", out var fixture) || fixture.Shape is not PhysShapeCircle circle)
            return false;

        radius = circle.Radius;
        return true;
    }

    private void ExecuteDefensiveFire(Entity<AirDefenseComponent> ent, EntityUid target)
    {
        if (!TryComp<GunComponent>(ent, out var gun))
            return;

        var targetCoords = Transform(target).Coordinates;

        if (!_gun.AttemptShoot(ent, ent, gun, targetCoords))
            return;

        RotateToTarget(ent, target);

        if (_random.Prob(ent.Comp.MissProbability))
            return;

        PredictedSpawnAtPosition(EffectEmpPulse, targetCoords);
        PredictedDel(target);
    }

    private void RotateToTarget(EntityUid uid, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        var shooterPos = _xform.GetWorldPosition(uid);
        var targetPos = _xform.GetWorldPosition(target);

        var direction = targetPos - shooterPos;
        if (direction.LengthSquared() > 0.001f)
        {
            _xform.SetWorldRotation(uid, direction.ToWorldAngle());
        }
    }
}

[Serializable, NetSerializable]
public enum AirDefenseCellVisuals : byte
{
    HasPowerCell,
}
