using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Melee.Events;


[Serializable, NetSerializable]
public sealed class MeleeHitBlockAttemptEvent() : EntityEventArgs
{
    public bool Cancelled = false;
    public NetEntity? blocker = null;
}
