// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.SS220.Felinids.Events;

public sealed class FelinidsRecoilEvent : EntityEventArgs
{
    public readonly EntityUid Shooter;
    public readonly EntityUid Gun;
    public readonly EntityCoordinates FromCoordinates;
    public readonly EntityCoordinates ToCoordinates;
    public readonly PhysicsComponent ShooterPhysics;

    public FelinidsRecoilEvent(EntityUid shoter, EntityUid gun, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, PhysicsComponent shooterPhysics)
    {
        Shooter = shoter;
        Gun = gun;
        FromCoordinates = fromCoordinates;
        ToCoordinates = toCoordinates;
        ShooterPhysics = shooterPhysics;
    }
}
