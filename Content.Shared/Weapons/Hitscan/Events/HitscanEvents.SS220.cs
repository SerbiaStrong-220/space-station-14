using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Hitscan.Events;


[ByRefEvent]
public struct AttemptHitscanRaycastHitEvent
{
    public EntityUid HitScanEntity;
    public bool Cancelled;
}
