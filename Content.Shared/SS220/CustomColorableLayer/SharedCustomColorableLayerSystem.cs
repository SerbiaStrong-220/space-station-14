// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Interaction;
using Content.Shared.SprayPainter.Components;

namespace Content.Shared.SS220.CustomColorableLayer;

public abstract partial class SharedCustomColorableLayerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomColorableLayerComponent, AfterInteractUsingEvent>(OnInteractedWith);
    }

    protected virtual void OnInteractedWith(Entity<CustomColorableLayerComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<SprayPainterComponent>(args.Used, out var painterComp) || painterComp.SelectedDecalColor == null)
            return;

        if (painterComp.SelectedDecalColor != null)
        {
            ent.Comp.ColoredLayerColor = (Color)painterComp.SelectedDecalColor;
            return;
        }

        if (painterComp.ColorPalette.ContainsKey(painterComp.PickedColor))
            ent.Comp.ColoredLayerColor = painterComp.ColorPalette[painterComp.PickedColor];
    }
}
