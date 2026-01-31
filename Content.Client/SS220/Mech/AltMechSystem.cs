using Content.Client.SS220.FieldShield;
using Content.Shared.Mech;
using Content.Shared.SS220.FieldShield;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Mech;

/// <inheritdoc/>
public sealed class AltMechSystem : SharedAltMechSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechPartComponent, AfterAutoHandleStateEvent>(OnPartInserted);
        SubscribeLocalEvent<AltMechComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<AltMechComponent, ComponentInit>(OnComponentInit);

    }

    public readonly Dictionary<string, MechPartVisualLayers> partsVisuals = new Dictionary<string, MechPartVisualLayers>()
    {
        ["core"] = MechPartVisualLayers.Core,
        ["head"] = MechPartVisualLayers.Head,
        ["right-arm"] = MechPartVisualLayers.RightArm,
        ["left-arm"] = MechPartVisualLayers.LeftArm,
        ["chassis"] = MechPartVisualLayers.Chassis,
        ["power"] = MechPartVisualLayers.Power
    };

    private void OnAppearanceChanged(EntityUid uid, AltMechComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerExists((uid, args.Sprite), MechVisualLayers.Base))
            return;

        var state = component.BaseState;
        var drawDepth = DrawDepth.Mobs;
        if (component.BrokenState != null && _appearance.TryGetData<bool>(uid, MechVisuals.Broken, out var broken, args.Component) && broken)
        {
            state = component.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (component.OpenState != null && _appearance.TryGetData<bool>(uid, MechVisuals.Open, out var open, args.Component) && open)
        {
            state = component.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetRsiState((uid, args.Sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((uid, args.Sprite), (int)drawDepth);
    }

    private void OnComponentInit(Entity<AltMechComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp) || !TryComp(ent, out AppearanceComponent? appearance))
            return;

        foreach (MechPartVisualLayers layer in Enum.GetValues(typeof(MechPartVisualLayers)))
        {
            _sprite.LayerMapReserve((ent.Owner, spriteComp), layer);
            _sprite.LayerSetVisible((ent.Owner, spriteComp), layer, false);

            spriteComp.LayerSetShader(layer, "unshaded");
        }
    }

    private void OnPartInserted(Entity<MechPartComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.PartOwner, out var mechComp) || !TryComp(ent.Comp.PartOwner, out AppearanceComponent? appearance))
            return;

        if (!TryComp<SpriteComponent>(ent.Comp.PartOwner, out var spriteComp))
            return;

        if (_sprite.LayerMapTryGet( ((EntityUid)ent.Comp.PartOwner, spriteComp), partsVisuals[ent.Comp.slot], out var layer, true)
                && ent.Comp.AttachedSprite != null)
        {
            _sprite.LayerSetSprite( ((EntityUid)ent.Comp.PartOwner, spriteComp), layer, ent.Comp.AttachedSprite);
            _sprite.LayerSetVisible( ((EntityUid)ent.Comp.PartOwner, spriteComp), layer, true);
        }
    }
}

public enum MechPartVisualLayers : byte
{
    Core = 0,
    Head = 1,
    RightArm = 2,
    LeftArm = 3,
    Chassis = 4,
    Power = 5
}
