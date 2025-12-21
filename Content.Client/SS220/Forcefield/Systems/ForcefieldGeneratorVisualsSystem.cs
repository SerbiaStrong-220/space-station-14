// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Forcefield.Components;
using Content.Shared.SS220.Forcefield.Components;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Forcefield.Systems;

public sealed class ForcefieldGeneratorVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldGeneratorVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<ForcefieldGeneratorVisualsComponent> entity, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite is null)
            return;

        if (_appearance.TryGetData<bool>(entity, ForcefieldGeneratorVisual.Active, out var active) &&
            _sprite.LayerMapTryGet((entity, sprite), ForcefieldGeneratorVisual.Active, out _, false))
        {
            _sprite.LayerSetVisible((entity, sprite), ForcefieldGeneratorVisual.Active, active);
        }

        if (_appearance.TryGetData<float>(entity, ForcefieldGeneratorVisual.Charge, out var charge) &&
            _sprite.LayerMapTryGet((entity, sprite), ForcefieldGeneratorVisual.Charge, out _, false))
        {
            charge = Math.Clamp(charge, 0, 1f);
            var curStep = Math.Floor(charge * (entity.Comp.PowerSteps - 1));
            _sprite.LayerSetRsiState((entity, sprite), ForcefieldGeneratorVisual.Charge, $"{entity.Comp.PowerState}_{curStep}");
        }
    }
}
