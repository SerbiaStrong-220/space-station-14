// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.SS220.ChangeAppearanceOnActiveBlocking;

public sealed partial class ChangeAppearanceOnActiveBlockingSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
    }

    public void OnAppearanceChange(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var AppearanceComp))
            return;

        if (!TryComp<SpriteComponent>(ent.Owner, out var SpriteComp))
            return;

        if (!_appearanceSystem.TryGetData<bool>(ent.Owner, ActiveBlockingVisuals.Enabled, out var enabled, args.Component))
            return;

        var modulateColor =
            _appearanceSystem.TryGetData<Color>(ent.Owner, ToggleableVisuals.Color, out var color, args.Component);

        if (args.Sprite != null && ent.Comp.SpriteLayer != null &&
            _spriteSystem.LayerMapTryGet((ent.Owner, SpriteComp), ent.Comp.SpriteLayer, out var layer, false))
        {
            _spriteSystem.LayerSetVisible((ent.Owner, SpriteComp), layer, enabled);

            if (modulateColor)
                _spriteSystem.LayerSetColor((ent.Owner, SpriteComp), ent.Comp.SpriteLayer, color);
        }

        _item.VisualsChanged(ent.Owner);
    }

    private void OnGetHeldVisuals(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        if(!_appearanceSystem.TryGetData<bool>(ent.Owner, ActiveBlockingVisuals.Enabled, out var enabled, appearance)
            || !enabled)
            return;

        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var modulateColor = _appearanceSystem.TryGetData<Color>(ent.Owner, ToggleableVisuals.Color, out var color, appearance);

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-toggle";

        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            if (modulateColor)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }
}
