using Content.Client.SS220.FieldShield;
using Content.Client.SS220.Mech.Ui;
using Content.Shared.Damage;
using Content.Shared.Mech;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.AltMech;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltMechComponent, AppearanceChangeEvent>(OnAppearanceChanged);

        SubscribeLocalEvent<AltMechComponent, ComponentInit>(OnComponentInit);
        SubscribeNetworkEvent<MechPartStatusChanged>(OnPartMoved);

        SubscribeLocalEvent<AltMechComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<AltMechComponent, EntRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<AltMechComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<AltMechComponent, OnMechExitEvent>(OnPilotEjected);

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

            //spriteComp.LayerSetShader(layer, "unshaded");
        }
    }

    private void OnInserted(Entity<AltMechComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        if (uiComp.ClientOpenInterfaces.ContainsKey(MechUiKey.Key) && uiComp.ClientOpenInterfaces[MechUiKey.Key] is AltMechBoundUserInterface)
        {
            var bui = (AltMechBoundUserInterface)uiComp.ClientOpenInterfaces[MechUiKey.Key];

            if (bui == null)
                return;

            bui.UpdateUI();
        }

    }

    private void OnRemoved(Entity<AltMechComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        if (uiComp.ClientOpenInterfaces.ContainsKey(MechUiKey.Key) && uiComp.ClientOpenInterfaces[MechUiKey.Key] is AltMechBoundUserInterface)
        {
            var bui = (AltMechBoundUserInterface)uiComp.ClientOpenInterfaces[MechUiKey.Key];

            if (bui == null)
                return;

            bui.UpdateUI();
        }

    }

    private void OnDamageChanged(Entity<AltMechComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        if (uiComp.ClientOpenInterfaces.ContainsKey(MechUiKey.Key) && uiComp.ClientOpenInterfaces[MechUiKey.Key] is AltMechBoundUserInterface)
        {
            var bui = (AltMechBoundUserInterface)uiComp.ClientOpenInterfaces[MechUiKey.Key];

            if (bui == null)
                return;

            bui.UpdateUI();
        }

    }

    private void OnPartMoved(MechPartStatusChanged args)
    {
        if (!TryGetEntity(args.Mech, out var localMech) || !TryGetEntity(args.Part, out var localPart))
            return;

        if (!TryComp<AltMechComponent>(localMech, out var mechComp) || !TryComp(localMech, out AppearanceComponent? appearance))
            return;

        if (!TryComp<SpriteComponent>(localMech, out var spriteComp) || spriteComp == null)
            return;

        if (!TryComp<MechPartComponent>(localPart, out var partComp))
            return;

        if (_sprite.LayerMapTryGet(((EntityUid)localMech, spriteComp), partsVisuals[partComp.slot], out var layer, true))
        {
            if(args.Attached == false)
            {
                _sprite.LayerSetVisible(((EntityUid)localMech, spriteComp), layer, false);
                return;
            }
            if(partComp.AttachedSprite != null)
            {
                _sprite.LayerSetSprite(((EntityUid)localMech, spriteComp), layer, partComp.AttachedSprite);
                _sprite.LayerSetVisible(((EntityUid)localMech, spriteComp), layer, true);
            }
        }
            //_sprite.LayerSetVisible(((EntityUid)localMech, spriteComp), layer, false);
    }

    private void OnPilotEjected(Entity<AltMechComponent> ent, ref OnMechExitEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        if (uiComp.ClientOpenInterfaces.ContainsKey(MechUiKey.Key) && uiComp.ClientOpenInterfaces[MechUiKey.Key] is AltMechBoundUserInterface)
        {
            var bui = (AltMechBoundUserInterface)uiComp.ClientOpenInterfaces[MechUiKey.Key];

            if (bui == null)
                return;

            bui.Close();
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
