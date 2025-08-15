// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.EntityEffects.EffectConditions;

namespace Content.Server.EntityEffects;

public sealed partial class EntityEffectSystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private void OnCheckComponentCondition(ref CheckEntityEffectConditionEvent<HasComponentsCondition> args)
    {
        if (args.Condition.Components.Length == 0)
        {
            args.Result = true;
            return;
        }

        var condition = args.Condition.RequireAll;
        foreach (var component in args.Condition.Components)
        {
            var availability = _componentFactory.GetComponentAvailability(component);
            if (!_componentFactory.TryGetRegistration(component, out var registration) ||
                availability != ComponentAvailability.Available)
                continue;

            if (HasComp(args.Args.TargetEntity, registration.Type))
            {
                if (args.Condition.RequireAll)
                    continue;

                condition = true;
                break;
            }

            if (!args.Condition.RequireAll)
                continue;

            condition = false;
            break;
        }

        args.Result = condition ^ args.Condition.Inverted;
    }
}
