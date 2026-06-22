// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Client.SS220.Mech.Ui;
using Content.Client.UserInterface.Systems.DamageOverlays.Overlays;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
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
        ["core"] = MechPartVisualLayers.Core,
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

        foreach (MechPartVisualLayers layer in Enum.GetValues(typeof(MechPartVisualLayers)))
        {
            _sprite.LayerMapReserve((ent.Owner, spriteComp), layer);
            _sprite.LayerSetVisible((ent.Owner, spriteComp), layer, false);
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
        if (!TryGetEntity(args.Mech, out var localMech))
            return;

        if (!TryComp<SpriteComponent>(localMech, out var spriteComp) || spriteComp == null)
            return;

        if (!TryGetEntity(args.Part, out var localPart) && args.Slot != null)
        {
            if (_sprite.LayerMapTryGet(((EntityUid)localMech, spriteComp), partsVisuals[args.Slot], out var layerOfMissingPart, true))
                _sprite.LayerSetVisible(((EntityUid)localMech, spriteComp), layerOfMissingPart, false);
            return;

        }

        if (!TryComp<AltMechComponent>(localMech, out var mechComp) || !TryComp(localMech, out AppearanceComponent? appearance))
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
    Chassis = 2,
    RightArm = 3,
    LeftArm = 4,
    Power = 5
}
