// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.SS220.ItemExtension;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class ItemExtensionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemExtensionComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);

        base.Initialize();
    }

    public void OnPickupAttempt(Entity<ItemExtensionComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            if (parametersComp.ParameterDict.ContainsKey(Parameter.Strength))
                userStrength = parametersComp.ParameterDict[Parameter.Strength];

        if (TryComp<HandsComponent>(args.User, out var handsComp) &&
            handsComp.ActiveHandId != null &&
            _handsSystem.TryGetHand(args.User, handsComp.ActiveHandId, out var activeHand) &&
            activeHand.Value.StrengthModifier != null)
            userStrength = (FixedPoint2)activeHand.Value.StrengthModifier;

        if (userStrength < ent.Comp.MinimalStrengthToPickUp)
            args.Cancel();
    }
}
