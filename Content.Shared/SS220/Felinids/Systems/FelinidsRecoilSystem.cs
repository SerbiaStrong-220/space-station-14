// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Felinids.Components;
using Content.Shared.SS220.Felinids.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class FelinidsRecoilSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidsRecoilComponent, FelinidsRecoilEvent>(OnFelinidsRecoil);
    }

    private void OnFelinidsRecoil(Entity<FelinidsRecoilComponent> ent, ref FelinidsRecoilEvent args)
    {
        CauseImpulse(args.FromCoordinates, args.ToCoordinates, ent.Comp, args.Shooter, args.ShooterPhysics);
    }

    public void CauseImpulse(EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, FelinidsRecoilComponent recoilComp, EntityUid user, PhysicsComponent userPhysics)
    {
        var fromMap = _transformSystem.ToMapCoordinates(fromCoordinates).Position;
        var toMap = _transformSystem.ToMapCoordinates(toCoordinates).Position;
        var shotDirection = (toMap - fromMap).Normalized();
        var impulseVector = shotDirection;

        if (userPhysics.BodyStatus == 0)
        {
            impulseVector *= recoilComp.ImpulseOnGround;
        }
        else
            impulseVector *= recoilComp.ImpulseInAir;

        _physics.ApplyLinearImpulse(user, -impulseVector, body: userPhysics);
    }

}
