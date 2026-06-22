// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.SS220.ItemExtension;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class ItemExtensionSystem : EntitySystem
{
    [Dependency] private readonly PhysicalParametersSystem _parametersSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemExtensionComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);

        base.Initialize();
    }

    public void OnPickupAttempt(Entity<ItemExtensionComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        if (userStrength < ent.Comp.MinimalStrengthToPickUp)
            args.Cancel();
    }
}
