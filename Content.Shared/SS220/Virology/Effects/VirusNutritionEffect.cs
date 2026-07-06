// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusNutritionEffect : IVirusEffect
{
    /// <summary>Extra hunger drained per tick, as a fraction of base hunger decay rate.</summary>
    [DataField]
    public float Hunger;

    /// <summary>Extra thirst drained per tick, as a fraction of base thirst decay rate.</summary>
    [DataField]
    public float Thirst;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        if (Hunger != 0f && args.EntityManager.TryGetComponent<HungerComponent>(args.Carrier, out var hunger))
            args.EntityManager.System<HungerSystem>().ModifyHunger(args.Carrier, -hunger.BaseDecayRate * Hunger, hunger);

        if (Thirst != 0f && args.EntityManager.TryGetComponent<ThirstComponent>(args.Carrier, out var thirst))
            args.EntityManager.System<ThirstSystem>().ModifyThirst(args.Carrier, thirst, -thirst.BaseDecayRate * Thirst);
    }
}
