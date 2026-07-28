// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Client.SS220.Mech.Ui;
using Content.Client.UserInterface.Systems.DamageOverlays.Overlays;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Parts.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.SS220.Mech;

/// <inheritdoc/>
public sealed partial class AltMechSystem : SharedAltMechSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    private DamageOverlay _damageOverlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltMechComponent, AppearanceChangeEvent>(OnAppearanceChanged);

        //SubscribeLocalEvent<AltMechComponent, ComponentStartup>(OnComponentStartup);
        //SubscribeNetworkEvent<MechPartStatusChanged>(OnPartMoved);

        SubscribeLocalEvent<AltMechComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<AltMechComponent, EntRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<AltMechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechPartComponent, DamageChangedEvent>(OnPartDamageChanged);

        SubscribeLocalEvent<AltMechComponent, OnMechExitEvent>(OnPilotEjected);

        _damageOverlay = new DamageOverlay();
        SubscribeLocalEvent<AltMechComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<AltMechComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AltMechPilotComponent, MobThresholdChecked>(OnThresholdCheck);

    }

    public readonly Dictionary<string, MechPartVisualLayers> partsVisuals = new Dictionary<string, MechPartVisualLayers>()
    {
        ["head"] = MechPartVisualLayers.Head,
        ["right-arm"] = MechPartVisualLayers.RightArm,
        ["left-arm"] = MechPartVisualLayers.LeftArm,
        ["chassis"] = MechPartVisualLayers.Chassis,
        ["power"] = MechPartVisualLayers.Power
    };

    private void OnAppearanceChanged(Entity<AltMechComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerExists((ent.Owner, args.Sprite), MechVisualLayers.Base))
            return;

        var state = ent.Comp.BaseState;
        var drawDepth = DrawDepth.Mobs;
        if (ent.Comp.BrokenState != null && _appearance.TryGetData<bool>(ent.Owner, MechVisuals.Broken, out var broken, args.Component) && broken)
        {
            state = ent.Comp.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (ent.Comp.OpenState != null && _appearance.TryGetData<bool>(ent.Owner, MechVisuals.Open, out var open, args.Component) && open)
        {
            state = ent.Comp.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetRsiState((ent.Owner, args.Sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((ent.Owner, args.Sprite), (int)drawDepth);
    }

    protected override void OnStartup(Entity<AltMechComponent> ent, ref ComponentStartup args)
    {
        base.OnStartup(ent, ref args);

        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp) || !TryComp(ent, out AppearanceComponent? appearance))
            return;

        _sprite.LayerSetColor((ent, spriteComp), ent.Comp.AttachedColoredSpriteLayer, ent.Comp.ColoredSpriteColor);

        foreach (var partContainer in ent.Comp.ContainerDict)
        {
            if (partContainer.Value.ContainedEntity is not { Valid: true } partEntityValid || !TryComp<MechPartComponent>(partEntityValid, out var partComp))
            {
                if (partContainer.Key != null && _sprite.LayerMapTryGet((ent.Owner, spriteComp), partsVisuals[partContainer.Key], out var layerOfMissingPart, true))
                    _sprite.LayerSetVisible((ent.Owner, spriteComp), layerOfMissingPart, false);

                continue;
            }

            ProcessPartVisuals(ent, (partEntityValid, partComp), true, partContainer.Key);
        }
    }

    private void OnInserted(Entity<AltMechComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (TryComp<MechPartComponent>(args.Entity, out var partComp))
            ProcessPartVisuals(ent, (args.Entity, partComp), true, partComp.slot);

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
        if (TryComp<MechPartComponent>(args.Entity, out var partComp))
            ProcessPartVisuals(ent, (args.Entity, partComp), false, partComp.slot);

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

    private void OnPartDamageChanged(Entity<MechPartComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.PartOwner == null)
            return;

        var mech = (EntityUid)ent.Comp.PartOwner;

        if (mech != _playerManager.LocalEntity)
            return;

        if (!TryComp<UserInterfaceComponent>(mech, out var uiComp))
            return;

        if (uiComp.ClientOpenInterfaces.ContainsKey(MechUiKey.Key) && uiComp.ClientOpenInterfaces[MechUiKey.Key] is AltMechBoundUserInterface)
        {
            var bui = (AltMechBoundUserInterface)uiComp.ClientOpenInterfaces[MechUiKey.Key];

            if (bui == null)
                return;

            bui.UpdateUI();
        }

    }

    private void ProcessPartVisuals(Entity<AltMechComponent> mech, Entity<MechPartComponent> part, bool attached, string? slot)
    {
        if (!TryComp<SpriteComponent>(mech, out var spriteComp) || spriteComp == null)
            return;

        SpriteSpecifier? spriteToAdd = part.Comp.AttachedSprite;

        SpriteSpecifier? coloredSpriteToAdd = part.Comp.AttachedColoredSprite;

        if (part.Comp.slot == "head")
        {
            _sprite.LayerSetVisible((mech, spriteComp), mech.Comp.AttachedHeadSpriteLayer, attached);
            _sprite.LayerSetVisible((mech, spriteComp), mech.Comp.AttachedHeadColoredSpriteLayer, attached);
            _sprite.LayerSetVisible((mech, spriteComp), mech.Comp.CameraVisLayer, attached);

            _sprite.LayerSetColor((mech, spriteComp), mech.Comp.AttachedHeadColoredSpriteLayer, part.Comp.ColoredSpriteColor);

            if (TryComp<MechOpticsComponent>(part, out var opticsComp))
                _sprite.LayerSetColor((mech, spriteComp), mech.Comp.CameraVisLayer, opticsComp.CameraLayerColor);

            return;
        }

        if (_sprite.LayerMapTryGet((mech, spriteComp), partsVisuals[part.Comp.slot], out var layer, true))
        {
            _sprite.LayerSetVisible((mech, spriteComp), layer, attached);
            if (attached)
            {
                if (spriteToAdd != null)
                    _sprite.LayerSetSprite((mech, spriteComp), layer, spriteToAdd);
            }
        }

        if (coloredSpriteToAdd != null && _sprite.LayerMapTryGet((mech, spriteComp), partsVisuals[part.Comp.slot] + 1, out var layerColored, true))
        {
            _sprite.LayerSetVisible((mech, spriteComp), layerColored, attached);
            if (attached)
            {
                if (coloredSpriteToAdd != null)
                    _sprite.LayerSetSprite((mech, spriteComp), layerColored, coloredSpriteToAdd);

                _sprite.LayerSetColor((mech, spriteComp), layerColored, part.Comp.ColoredSpriteColor);
            }
        }
    }

    protected override void OnMechInteractedWith(Entity<AltMechComponent> ent, ref AfterInteractUsingEvent args)
    {
        base.OnMechInteractedWith(ent, ref args);

        if (TryComp<SpriteComponent>(ent.Owner, out var spriteComp))
            _sprite.LayerSetColor((ent, spriteComp), ent.Comp.AttachedColoredSpriteLayer, ent.Comp.ColoredSpriteColor);
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
