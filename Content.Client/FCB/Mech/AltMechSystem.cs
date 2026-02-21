// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Client.FCB.Mech.Ui;
using Content.Client.UserInterface.Systems.DamageOverlays.Overlays;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Parts.Components;
using Content.Shared.FCB.Mech.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mech;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.FCB.Mech;

/// <inheritdoc/>
public sealed class AltMechSystem : SharedAltMechSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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

    //There are going to be LOTS OF GARBAGE here. Every overlay that is applied on the pilot but not on the mech must me manually rewritten here. This is an awful solution but or this or manually editing every officials' file
    private void OnPlayerAttach(Entity<AltMechComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        DamageOverlayInit(args);
        //BlurOverlayInit(args);
    }

    private void DamageOverlayInit(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();

        if (!EntityManager.TryGetComponent<AltMechComponent>(args.Entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        if (!EntityManager.TryGetComponent<MobStateComponent>(mechComp.PilotSlot.ContainedEntity, out var mobState))
            return;

        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays(args.Entity, mobState);
        _overlay.AddOverlay(_damageOverlay);
    }

    private void BlurOverlayInit(LocalPlayerAttachedEvent args)
    {
        if (!EntityManager.TryGetComponent<AltMechComponent>(args.Entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        if (!EntityManager.TryGetComponent<BlurryVisionComponent>(mechComp.PilotSlot.ContainedEntity, out var blurry))
            return;

        EnsureComp<BlurryVisionComponent>(args.Entity, out var mechBlurry);

    }

    private void OnPlayerDetached(Entity<AltMechComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_damageOverlay);
        ClearOverlay();
        RemComp<BlurryVisionComponent>(args.Entity);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, args.Component);
    }

    private void OnThresholdCheck(Entity<AltMechPilotComponent> ent, ref MobThresholdChecked args)
    {

        if (!(EntityManager.TryGetComponent(args.Target, out AltMechPilotComponent? pilot)))
            return;

        if (pilot.Mech != _playerManager.LocalEntity)
            return;

        UpdateOverlays(pilot.Mech, args.MobState, (DamageableComponent?) args.Damageable, args.Threshold);
    }

    private void ClearOverlay()
    {
        _damageOverlay.DeadLevel = 0f;
        _damageOverlay.CritLevel = 0f;
        _damageOverlay.PainLevel = 0f;
        _damageOverlay.OxygenLevel = 0f;
    }

    private void UpdateOverlays(EntityUid entity, MobStateComponent? mobState, DamageableComponent? damageable = null, MobThresholdsComponent? thresholds = null)
    {
        if (!EntityManager.TryGetComponent<AltMechComponent>(entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        var pilot = mechComp.PilotSlot.ContainedEntity;

        if (mobState == null && !EntityManager.TryGetComponent(pilot, out mobState) ||
            thresholds == null && !EntityManager.TryGetComponent(pilot, out thresholds) ||
            damageable == null && !EntityManager.TryGetComponent(pilot, out damageable))
            return;

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay();
            return; //this entity intentionally has no overlays
        }

        if (!_mobThresholdSystem.TryGetIncapThreshold((EntityUid)pilot, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!

        var critThreshold = foundThreshold.Value;
        _damageOverlay.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
                {
                    FixedPoint2 painLevel = 0;
                    _damageOverlay.PainLevel = 0;

                    //if (!_statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(pilot, out _)) uncomment on upstream
                    if(TryComp<PainNumbnessComponent>(pilot, out var numbnessComp))
                    {
                        foreach (var painDamageType in damageable.PainDamageGroups)
                        {
                            damageable.DamagePerGroup.TryGetValue(painDamageType, out var painDamage);
                            painLevel += painDamage;
                        }
                        _damageOverlay.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                        if (_damageOverlay.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                        {
                            _damageOverlay.PainLevel = 0;
                        }
                    }

                    if (damageable.DamagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                    {
                        _damageOverlay.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                    }

                    _damageOverlay.CritLevel = 0;
                    _damageOverlay.DeadLevel = 0;
                    break;
                }
            case MobState.Critical:
                {
                    if (!_mobThresholdSystem.TryGetDeadPercentage((EntityUid)pilot,
                            FixedPoint2.Max(0.0, damageable.TotalDamage), out var critLevel))
                        return;
                    _damageOverlay.CritLevel = critLevel.Value.Float();

                    _damageOverlay.PainLevel = 0;
                    _damageOverlay.DeadLevel = 0;
                    break;
                }
            case MobState.Dead:
                {
                    _damageOverlay.PainLevel = 0;
                    _damageOverlay.CritLevel = 0;
                    break;
                }
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
