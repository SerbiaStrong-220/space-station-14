using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;


[Serializable, NetSerializable]
public sealed class ThrowableProjectileBlockAttemptEvent(DamageSpecifier? damage) : EntityEventArgs
{
    public bool Cancelled = false;
    public DamageSpecifier? Damage = damage;
}
