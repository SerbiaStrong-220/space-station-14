// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Interaction;
using Content.Shared.SS220.CustomColorableLayer;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CustomColorableLayer;

public sealed partial class CustomColorableLayerSystem : SharedCustomColorableLayerSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    protected override void OnInteractedWith(Entity<CustomColorableLayerComponent> ent, ref AfterInteractUsingEvent args)
    {
        base.OnInteractedWith(ent, ref args);

        if (TryComp<SpriteComponent>(ent.Owner, out var spriteComp))
            _sprite.LayerSetColor((ent, spriteComp), ent.Comp.AttachedColoredSpriteLayer, ent.Comp.ColoredLayerColor);
    }
}
