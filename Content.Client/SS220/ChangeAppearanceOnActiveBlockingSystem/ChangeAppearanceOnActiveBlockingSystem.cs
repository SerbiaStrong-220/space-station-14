using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.SS220.ChangeAppearanceOnActiveBLocking;

public sealed partial class ChangeAppearanceOnActiveBlockingSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
<<<<<<< HEAD

    public override void Initialize()
    {
        base.Initialize();

=======
    
    public override void Initialize()
    {
        base.Initialize();
        
>>>>>>> e16a8587772b7fe20c0b53f76b2840836102ffb7
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
    }

    public void OnAppearanceChange(EntityUid uid, ChangeAppearanceOnActiveBlockingComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var AppearanceComp))
            return;

        if (!TryComp<SpriteComponent>(uid, out var SpriteComp))
            return;

        if (!_appearanceSystem.TryGetData<bool>(uid, ActiveBlockingVisuals.Enabled, out var enabled, args.Component))
            return;
<<<<<<< HEAD

=======
            
>>>>>>> e16a8587772b7fe20c0b53f76b2840836102ffb7
        var modulateColor =
            _appearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, args.Component);

        if (args.Sprite != null && component.SpriteLayer != null &&
            _spriteSystem.LayerMapTryGet((uid, SpriteComp), component.SpriteLayer, out var layer, false))
        {
            _spriteSystem.LayerSetVisible((uid, SpriteComp), layer, enabled);

            if (modulateColor)
                _spriteSystem.LayerSetColor((uid, SpriteComp), component.SpriteLayer, color);
        }
<<<<<<< HEAD

=======
        
>>>>>>> e16a8587772b7fe20c0b53f76b2840836102ffb7
        _item.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, ChangeAppearanceOnActiveBlockingComponent component, GetInhandVisualsEvent args)
    {
<<<<<<< HEAD
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        if(!_appearanceSystem.TryGetData<bool>(uid, ActiveBlockingVisuals.Enabled, out var enabled, appearance)
=======
        if (!TryComp(uid, out AppearanceComponent? appearance)
            return;
            
         if (!_appearanceSystem.TryGetData<bool>(uid, ActiveBlockingVisuals.Enabled, out var enabled, appearance)
>>>>>>> e16a8587772b7fe20c0b53f76b2840836102ffb7
            || !enabled)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var modulateColor = _appearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, appearance);

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
