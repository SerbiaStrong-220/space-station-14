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

        SubscribeLocalEvent<AltMechComponent, ComponentInit>(OnComponentInit);
        SubscribeNetworkEvent<MechPartStatusChanged>(OnPartMoved);

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

    private void OnComponentInit(Entity<AltMechComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp) || !TryComp(ent, out AppearanceComponent? appearance))
            return;

        _sprite.LayerSetColor((ent, spriteComp), ent.Comp.AttachedColoredSpriteLayer, ent.Comp.ColoredSpriteColor);
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

    private void OnPartMoved(MechPartStatusChanged args)
    {
        if (!TryGetEntity(args.Mech, out var localMech) || localMech is not { Valid: true } localMechValidated)
            return;

        if (!TryComp<SpriteComponent>(localMechValidated, out var spriteComp) || spriteComp == null)
            return;

        if (!TryGetEntity(args.Part, out var localPart) && args.Slot != null)
        {
            if (_sprite.LayerMapTryGet((localMechValidated, spriteComp), partsVisuals[args.Slot], out var layerOfMissingPart, true))
                _sprite.LayerSetVisible((localMechValidated, spriteComp), layerOfMissingPart, false);
            return;

        }

        if (!TryComp<AltMechComponent>(localMechValidated, out var mechComp) || !TryComp(localMechValidated, out AppearanceComponent? appearance))
            return;

        if (!TryComp<MechPartComponent>(localPart, out var partComp))
            return;

        if (_sprite.LayerMapTryGet((localMechValidated, spriteComp), partsVisuals[partComp.slot], out var layer, true))
        {
            _sprite.LayerSetVisible((localMechValidated, spriteComp), layer, args.Attached);
            if (args.Attached)
            {
                if (partComp.AttachedSprite != null)
                    _sprite.LayerSetSprite((localMechValidated, spriteComp), layer, partComp.AttachedSprite);
            }
        }
        if (partComp.AttachedColoredSprite != null && _sprite.LayerMapTryGet((localMechValidated, spriteComp), partsVisuals[partComp.slot] + 1, out var layerColored, true))
        {
            _sprite.LayerSetVisible((localMechValidated, spriteComp), layerColored, args.Attached);
            if (args.Attached)
            {
                if (partComp.AttachedColoredSprite != null)
                    _sprite.LayerSetSprite((localMechValidated, spriteComp), layerColored, partComp.AttachedColoredSprite);

                _sprite.LayerSetColor((localMechValidated, spriteComp), layerColored, partComp.ColoredSpriteColor);
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
