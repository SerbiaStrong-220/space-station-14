// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Placeable;
using Content.Shared.SS220.Cooking.Grilling;

namespace Content.Server.SS220.Cooking.Grilling;

/// <summary>
/// This handles grill cooking
/// </summary>
public sealed class GrillSystem : SharedGrillSystem
{
    // 473f - ideal grill temp in Kelvins
    private const float IdealGrillingTemperature = (200 + Atmospherics.T0C);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GrillComponent, ItemPlacerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out _,
                   out var grill,
                   out var placer,
                   out var power))
        {
            if (!power.Powered || !grill.IsGrillOn)
                continue;

            foreach (var ent in placer.PlacedEntities)
            {
                if (TryComp<InternalTemperatureComponent>(ent, out var temp) &&
                    TryComp<GrillableComponent>(ent, out var grillable))
                {
                    // Cook faster, when approaching 200 degrees Celsius
                    var currentAirTemperature = Math.Clamp(temp.Temperature, Atmospherics.FireMinimumTemperatureToExist, IdealGrillingTemperature);
                    var cookingSpeed = float.Lerp(0.5f, 1f, (currentAirTemperature - Atmospherics.FireMinimumTemperatureToExist) / 100f);

                    grillable.CurrentCookTime += (cookingSpeed + grill.CookingMultiplier) * frameTime;
                    Dirty(ent, grillable);

                    var ev = new CookTimeChanged(ent);
                    RaiseLocalEvent(ent, ref ev);
                }
            }
        }
    }
}
