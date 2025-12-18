namespace Content.Shared.Weapons.Melee.Events;

public sealed partial class DisarmChanceModifierEvent : EntityEventArgs
{
    public float BaseChance;
    public float Bonus;

    public DisarmChanceModifierEvent(float chance)
    {
        BaseChance = chance;
    }
}
