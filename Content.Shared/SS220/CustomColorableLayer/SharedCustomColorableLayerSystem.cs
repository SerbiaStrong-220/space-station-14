// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CustomColorableLayer;

public abstract partial class SharedCustomColorableLayerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomColorableLayerComponent, AfterInteractUsingEvent>(OnInteractedWith);
        SubscribeLocalEvent<CustomColorableLayerComponent, CustomColorPaintEvent>(OnPaintDoAfter);
    }

    protected void OnInteractedWith(Entity<CustomColorableLayerComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<SprayPainterComponent>(args.Used, out var painterComp) || painterComp.SelectedDecalColor == null)
            return;

        Color desiredColor = Color.White;

        if (painterComp.ColorPalette.ContainsKey(painterComp.PickedColor))
            desiredColor = painterComp.ColorPalette[painterComp.PickedColor];

        if (painterComp.SelectedDecalColor != null)
            desiredColor = (Color)painterComp.SelectedDecalColor;

        var doAfterEv = new CustomColorPaintEvent(desiredColor);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user: args.User, delay: ent.Comp.TimeToPaint, doAfterEv, eventTarget: ent.Owner, target: ent.Owner, used: args.Used)
        {
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    protected virtual void OnPaintDoAfter(Entity<CustomColorableLayerComponent> ent, ref CustomColorPaintEvent args)
    {
        ent.Comp.ColoredLayerColor = args.DesiredColor;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class CustomColorPaintEvent : DoAfterEvent
{
    public Color DesiredColor = Color.White;

    public CustomColorPaintEvent(Color desiredColor)
    {
        DesiredColor = desiredColor;
    }

    public override DoAfterEvent Clone() => this;
}
