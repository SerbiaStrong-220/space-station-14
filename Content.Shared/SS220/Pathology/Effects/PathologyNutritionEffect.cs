// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Drains the host's hunger and/or thirst each tick, each as a fraction of that need's base decay rate.
/// Leave a field at 0 to skip it; set both to drain hunger and thirst at once. Negative values slow the
/// need instead (-1 cancels the base decay, so it stops growing).
/// </summary>
public sealed partial class PathologyNutritionEffect : IPathologyEffect
{
    /// <summary>Extra hunger drained per tick, as a fraction of the base hunger decay rate.</summary>
    [DataField]
    public float Hunger;

    /// <summary>Extra thirst drained per tick, as a fraction of the base thirst decay rate.</summary>
    [DataField]
    public float Thirst;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        if (Hunger != 0f && args.EntityManager.TryGetComponent<HungerComponent>(args.Target, out var hunger))
            args.EntityManager.System<HungerSystem>().ModifyHunger(args.Target, -hunger.BaseDecayRate * Hunger, hunger);

        if (Thirst != 0f && args.EntityManager.TryGetComponent<ThirstComponent>(args.Target, out var thirst))
            args.EntityManager.System<ThirstSystem>().ModifyThirst(args.Target, thirst, -thirst.BaseDecayRate * Thirst);
    }
}
