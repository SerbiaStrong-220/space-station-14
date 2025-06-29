// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.SS220.EntityEffects.EffectConditions;

[UsedImplicitly]
public sealed partial class HasComponentsCondition : EntityEffectCondition
{
    [DataField(required: true)]
    public string[] Components;

    [DataField]
    public bool RequireAll = false;

    [DataField]
    public bool Inverted = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (Components.Length == 0)
            return true;

        var condition = RequireAll;
        var factory = IoCManager.Resolve<IComponentFactory>();
        foreach (var component in Components)
        {
            var availability = factory.GetComponentAvailability(component);
            if (!factory.TryGetRegistration(component, out var registration) ||
                availability != ComponentAvailability.Available)
                continue;
            else if (availability == ComponentAvailability.Unknown)
                Log.Error($"Unknown component name {component} passed to {this.ToString()}!");

            if (args.EntityManager.HasComponent(args.TargetEntity, registration.Type))
            {
                if (!RequireAll)
                {
                    condition = true;
                    break;
                }
            }
            else if (RequireAll)
            {
                condition = false;
                break;
            }
        }

        return condition ^ Inverted;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
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
