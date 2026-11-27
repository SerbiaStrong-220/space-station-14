using Content.Shared.Damage;

namespace Content.Server.SS220.DarkForces.Saint.Saintable.Events;

public sealed class OnSilverEntityHandInteract : HandledEntityEventArgs, ISaintEntityEvent
{
    public DamageSpecifier DamageOnCollide { get; set; }
    public bool PushOnCollide { get; set; }

    public bool IsHandled
    {
        get => Handled;
        set => Handled = value;
    }

    public OnSilverEntityHandInteract(DamageSpecifier damageOnCollide, bool pushOnCollide)
    {
        DamageOnCollide = damageOnCollide;
        PushOnCollide = pushOnCollide;
    }
}
