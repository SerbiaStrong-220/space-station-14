using Content.Shared.Damage;

namespace Content.Shared.SS220.DarkForces.Saint.Saintable;

/**
 * Метка для освященных предметов/структур
 * Или предметов/структур, которые можно освятить
 */
[RegisterComponent]
public sealed partial class SaintableComponent : Component
{
    [DataField("damageOnCollide")]
    public DamageSpecifier DamageOnCollide = new();

    [DataField("pushOnCollide")]
    public bool PushOnCollide;
}

[RegisterComponent]
public sealed partial class SaintedComponent : Component
{
    [DataField("damageOnCollide", required: true)]
    public DamageSpecifier DamageOnCollide = new();

    [DataField("pushOnCollide")]
    public bool PushOnCollide;
}
