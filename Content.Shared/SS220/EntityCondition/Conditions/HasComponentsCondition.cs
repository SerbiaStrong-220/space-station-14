// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityConditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.EffectConditions;

[UsedImplicitly]
public sealed partial class HasComponentsConditionSystem : EntityConditionSystem<MetaDataComponent, ComponentCondition>
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<ComponentCondition> args)
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

            if (HasComp(entity, registration.Type))
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

        args.Result = condition;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class ComponentCondition : EntityConditionBase<ComponentCondition>
{
    [DataField(required: true)]
    public string[] Components;

    [DataField]
    public bool RequireAll;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (Components.Length == 0)
            return string.Empty;

        var components = string.Empty;
        for (var i = 0; i < Components.Length; i++)
        {
            components += i + 1 != Components.Length
                ? Components[i] + ","
                : Components[i];
        }

        return Loc.GetString("reagent-effect-condition-guidebook-has-components", ("inverted", Inverted),
            ("requireAll", RequireAll), ("components", components));
    }
}
