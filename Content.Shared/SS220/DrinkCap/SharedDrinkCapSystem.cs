// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.SS220.DrinkCap;

public sealed partial class SharedDrinkCapSystem : EntitySystem
{
    [Dependency] private IngestionSystem _ingestion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdibleComponent, DrinkCapActionEvent>(OnDrinkAction);
    }

    private void OnDrinkAction(Entity<EdibleComponent> entity, ref DrinkCapActionEvent args)
    {
        args.Handled = _ingestion.TryIngest(args.Performer, args.Performer, entity);
    }
}

