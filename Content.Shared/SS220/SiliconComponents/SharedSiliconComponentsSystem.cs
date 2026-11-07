// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Lock;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Speech.Muting;
using Content.Shared.SS220.RadioUIVerb;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Security.Cryptography;

namespace Content.Shared.SS220.SiliconComponents;

public abstract partial class SharedSiliconComponentsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SiliconModuleSystem _module = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedWiresSystem _wires = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SiliconPartSystem _parts = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private static readonly LocId NotEnoughSpace = "silicon-component-not-enough-space";
    private static readonly LocId InstallationBegun = "silicon-component-begin-install";
    private static readonly LocId RemovalBegun = "silicon-component-begin-removal";
    private static readonly LocId BuiAltVerbOpen = "ui-silicon-open-wires";
    private static readonly LocId BuiAltVerbClose = "ui-silicon-close-wires";

    private static readonly ProtoId<DamageTypePrototype> EyeDamageType = "Heat";

    private static readonly string PartContainerPrefix = "silicon_component";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponentsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconComponentsComponent, AfterInteractUsingEvent>(OnSiliconInteractedWith);

        SubscribeLocalEvent<SiliconComponentsComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<SiliconComponentsComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);

        SubscribeLocalEvent<SiliconComponentsComponent, InstallSiliconPartEvent>(OnPartInstall);
        SubscribeLocalEvent<SiliconComponentsComponent, RemoveSiliconPartEvent>(OnPartRemove);

        SubscribeLocalEvent<SiliconPartComponent, SiliconPartStatusOnline>(OnPartOnline);
        SubscribeLocalEvent<SiliconPartComponent, SiliconPartStatusOffline>(OnPartOffline);

        SubscribeLocalEvent<SiliconPartComponent, ComponentGotInsertedIntoUser>(OnPartInserted);
        SubscribeLocalEvent<SiliconPartComponent, ComponentGotRemovedFromUser>(OnPartRemoved);

        SubscribeLocalEvent<SiliconComponentsComponent, InstallSiliconModuleEvent>(OnModInstall);
        SubscribeLocalEvent<SiliconComponentsComponent, RemoveSiliconModuleEvent>(OnModRemove);

        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectPartBuiMessage>(OnEjectPartBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconRemoveModuleBuiMessage>(OnRemoveModuleBuiMessage);

        SubscribeLocalEvent<SiliconComponentsComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<SiliconComponentsComponent, FlashAttemptEvent>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, GetEyeProtectionEvent>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowJobIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowHealthBarsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowHealthIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowHungerIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowThirstIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowMindShieldIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowSyndicateIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ShowCriminalRecordIconsComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<NoirOverlayComponent>>(RefRelayPartEvent);
        SubscribeLocalEvent<SiliconComponentsComponent, RefreshEquipmentHudEvent<ThermalSightComponent>>(RefRelayPartEvent);

        SubscribeLocalEvent<SiliconComponentsComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<SiliconComponentsComponent, EyeDamageChangedEvent>(OnEyeDamage);
    }

    private void OnComponentStartup(Entity<SiliconComponentsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(ent.Owner, out var containerManager))
            return;

        if (TryComp<BlindableComponent>(ent.Owner, out var ownerBlindableComp))
            _blindable.UpdateIsBlind(ent.Owner);

        foreach (PartType part in Enum.GetValues(typeof(PartType)))
        {
            if (ent.Comp.Parts.ContainsKey(part))
                continue;

            ent.Comp.Parts.Add(part, _container.EnsureContainer<ContainerSlot>(ent.Owner, PartContainerPrefix + "_" + Enum.GetName(part), containerManager));
        }

        ent.Comp.ModuleContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModuleContainerId, containerManager);

        Dirty(ent);

        if (_net.IsClient)
            return;

        if (TryGetOperational(ent.AsNullable()))
        {
            RemComp<StunnedComponent>(ent);
            RemComp<KnockedDownComponent>(ent);
            return;
        }

        EnsureComp<KnockedDownComponent>(ent);
        EnsureComp<StunnedComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SiliconComponentsComponent>();
        while (query.MoveNext(out var uid, out var siliconComponent))
        {
            if (curTime < siliconComponent.NextBatteryUpdate)
                continue;

            siliconComponent.NextBatteryUpdate = curTime + TimeSpan.FromSeconds(1);
            Dirty(uid, siliconComponent);

            // If we aren't drawing and suddenly get enough power to draw again, reenable.
            if (_powerCell.TryUseCharge(uid, siliconComponent.ChargeToUse.Float()))
            {
                if (siliconComponent.Online)
                    return;

                _statusEffects.TryRemoveStatusEffect(uid, "StatusEffectPowerOffline");
                RemComp<MutedComponent>(uid);
                siliconComponent.Online = true;
                Dirty(uid, siliconComponent);
                _blindable.UpdateIsBlind(uid);

                return;
            }

            if (!siliconComponent.Online)
                return;

            _statusEffects.TrySetStatusEffectDuration(uid, "StatusEffectPowerOffline", new TimeSpan(0, 0, 30));
            EnsureComp<MutedComponent>(uid);
            siliconComponent.Online = false;
            Dirty(uid, siliconComponent);
            _blindable.UpdateIsBlind(uid);
        }
    }

    private void OnSiliconInteractedWith(Entity<SiliconComponentsComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var used = args.Used;

        int occupiedSpace = 0;

        if (TryComp<SiliconPartComponent>(used, out var partComp))
            occupiedSpace = partComp.OccupiedSpace;

        if (TryComp<SiliconModuleComponent>(used, out var modComp))
            occupiedSpace = modComp.OccupiedSpace;

        if (occupiedSpace > ent.Comp.ModuleSpace)
        {
            _popup.PopupEntity(Loc.GetString(NotEnoughSpace), args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (partComp != null)
        {
            if (!ent.Comp.Parts.TryGetValue(partComp.PartType, out var container))
                return;

            if (container.ContainedEntity != null)
            {
                _popup.PopupEntity(Loc.GetString("silicon-component-slot-occupied"), args.User);
                return;
            }

            _popup.PopupEntity(Loc.GetString(InstallationBegun, ("item", args.Used)), ent.Owner);

            var partDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, partComp.TimeToInstall, new InstallSiliconPartEvent(partComp.PartType), ent.Owner, target: ent.Owner, used: args.Used)
            {
                BreakOnMove = true,
            };

            _doAfter.TryStartDoAfter(partDoAfterEventArgs);
            args.Handled = true;
        }

        if (modComp != null)
        {
            if (ent.Comp.ModuleContainer == null)
                return;

            _popup.PopupEntity(Loc.GetString(InstallationBegun, ("item", args.Used)), ent.Owner);

            var partDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, modComp.TimeToInstall, new InstallSiliconModuleEvent(), ent.Owner, target: ent.Owner, used: args.Used)
            {
                BreakOnMove = true,
            };

            _doAfter.TryStartDoAfter(partDoAfterEventArgs);
            args.Handled = true;
        }
    }

    private void OnEntityInserted(Entity<SiliconComponentsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        string containerID = args.Container.ID;

        if (TryComp<SiliconPartComponent>(args.Entity, out var partComp) && containerID.StartsWith(PartContainerPrefix))
        {
            partComp.PartOwner = ent.Owner;

            ent.Comp.ModuleSpace -= partComp.OccupiedSpace;

            var insertedEv = new ComponentGotInsertedIntoUser(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new ComponentInsertedIntoUser(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            _parts.RefreshAlerts(ent.AsNullable());

            UpdateUI(ent.AsNullable());
        }

        if (TryComp<SiliconModuleComponent>(args.Entity, out var moduleComp) && containerID.StartsWith(PartContainerPrefix))
        {
            moduleComp.ModuleOwner = ent.Owner;

            ent.Comp.ModuleSpace -= moduleComp.OccupiedSpace;

            var insertedEv = new SiliconModuleGotInserted(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new SiliconModuleInserted(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        Dirty(ent);
    }

    private void OnEntityRemoved(Entity<SiliconComponentsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        string containerID = args.Container.ID;

        if (TryComp<SiliconPartComponent>(args.Entity, out var partComp) && containerID.StartsWith(PartContainerPrefix))
        {
            partComp.PartOwner = null;

            ent.Comp.ModuleSpace += partComp.OccupiedSpace;

            var removedEv = new ComponentGotRemovedFromUser(ent.Owner);
            RaiseLocalEvent(args.Entity, ref removedEv);

            var userEv = new ComponentRemovedFromUser(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            _parts.RefreshAlerts(ent.AsNullable());

            UpdateUI(ent.AsNullable());
        }

        if (TryComp<SiliconModuleComponent>(args.Entity, out var moduleComp) && containerID.StartsWith(PartContainerPrefix))
        {
            moduleComp.ModuleOwner = null;

            ent.Comp.ModuleSpace += moduleComp.OccupiedSpace;

            var insertedEv = new SiliconModuleGotRemoved(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new SiliconModuleRemoved(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        Dirty(ent);
    }

    public virtual void UpdateUI(Entity<SiliconComponentsComponent?> ent) { }

    private void OnEjectPartBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconEjectPartBuiMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.DesiredPart, out var container))
            return;

        if (container.ContainedEntity is not { Valid: true } part)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(part, out var partComp))
            return;

        _popup.PopupEntity(Loc.GetString(RemovalBegun, ("item", args.Actor)), ent.Owner);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Actor, partComp.TimeToInstall, new RemoveSiliconPartEvent(args.DesiredPart), used: part, eventTarget: ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnEjectBatteryBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconEjectBatteryBuiMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_powerCell.TryEjectBatteryFromSlot(ent.Owner, out var powerCell, args.Actor))
            _hands.TryPickupAnyHand(args.Actor, powerCell.Value);
    }

    private void OnRemoveModuleBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconRemoveModuleBuiMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryGetEntity(args.Module, out var module) ||
            module is not { Valid: true } moduleValidated)
            return;

        if (!ent.Comp.ModuleContainer.Contains(moduleValidated))
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconModuleComponent>(moduleValidated, out var moduleComp))
            return;

        _popup.PopupEntity(Loc.GetString(RemovalBegun, ("item", args.Actor)), ent.Owner);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Actor, moduleComp.TimeToInstall, new RemoveSiliconModuleEvent(), used: moduleValidated, eventTarget: ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnDamageChanged(Entity<SiliconComponentsComponent> ent, ref DamageChangedEvent args)
    {
        _parts.RefreshAlerts(ent.AsNullable());
    }

    private void OnPartInstall(Entity<SiliconComponentsComponent> ent, ref InstallSiliconPartEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (args.Used is not { Valid: true } partValidated)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.Slot, out var container))
            return;

        if (container.ContainedEntity != null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(partValidated, out var partComp))
            return;

        _container.Insert(partValidated, container);

        Dirty(partValidated, partComp);
    }

    private void OnPartRemove(Entity<SiliconComponentsComponent> ent, ref RemoveSiliconPartEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.Slot, out var container))
            return;

        if (container.ContainedEntity is not { Valid: true } part)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(part, out var partComp))
            return;

        _container.Remove(part, container);
        _hands.TryPickupAnyHand(args.User, part);

        Dirty(part, partComp);

        args.Handled = true;
    }

    private void OnPartOnline(Entity<SiliconPartComponent> ent, ref SiliconPartStatusOnline args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Owner))
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) ||
                partComp.PartOwner is not { Valid: true } partOwnerValid)
            return;

        SetOperational(partOwnerValid, TryGetOperational(partOwnerValid));
    }

    private void OnPartOffline(Entity<SiliconPartComponent> ent, ref SiliconPartStatusOffline args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Owner))
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) ||
                partComp.PartOwner is not { Valid: true } partOwnerValid)
            return;

        SetOperational(partOwnerValid, TryGetOperational(partOwnerValid));
    }

    private void OnPartInserted(Entity<SiliconPartComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Owner))
            return;

        SetOperational(args.Owner, TryGetOperational(args.Owner));
    }

    private void OnPartRemoved(Entity<SiliconPartComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Owner))
            return;

        SetOperational(args.Owner, TryGetOperational(args.Owner));
    }

    private void OnModInstall(Entity<SiliconComponentsComponent> ent, ref InstallSiliconModuleEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (args.Used is not { Valid: true } modValidated)
            return;

        if (ent.Comp.ModuleContainer == null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconModuleComponent>(modValidated, out var modComp))
            return;

        if (!_module.CanInsertModule(ent, (modValidated, modComp)))
            return;

        _container.Insert(modValidated, ent.Comp.ModuleContainer);

        Dirty(modValidated, modComp);
    }

    private void OnModRemove(Entity<SiliconComponentsComponent> ent, ref RemoveSiliconModuleEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (args.Used is not { Valid: true } modValidated)
            return;

        if (ent.Comp.ModuleContainer == null)
            return;

        if (!ent.Comp.ModuleContainer.Contains(modValidated))
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        _container.Remove(modValidated, ent.Comp.ModuleContainer);
        _hands.TryPickupAnyHand(args.User, modValidated);

        if (TryComp<SiliconModuleComponent>(modValidated, out var modComp))
            Dirty(modValidated, modComp);

        args.Handled = true;
    }

    public bool TryGetPart(Entity<SiliconComponentsComponent?> ent, PartType type, out EntityUid? partUid)
    {
        partUid = null;

        if (!TryComp(ent.Owner, out ent.Comp))
            return false;

        if (!ent.Comp.Parts.TryGetValue(type, out var containerSlot) ||
            containerSlot.ContainedEntity is not { Valid: true } partEnt)
            return false;

        partUid = partEnt;

        return true;
    }

    protected void RefRelayPartEvent<T>(Entity<SiliconComponentsComponent> ent, ref T args) where T : ISiliconPartRelayEvent
    {
        RelayRefEvent(ent, ref args);
    }

    protected void RelayPartEvent<T>(Entity<SiliconComponentsComponent> ent, T args) where T : ISiliconPartRelayEvent
    {
        RelayEvent(ent, args);
    }

    public void RelayRefEvent<T>(Entity<SiliconComponentsComponent> ent, ref T args) where T : ISiliconPartRelayEvent
    {
        if (args.Parts == PartType.NONE)
            return;

        var ev = new PartRelayedEvent<T>(args, ent.Owner);

        foreach (var partType in ent.Comp.Parts.Keys)
        {
            if (TryGetPart(ent.AsNullable(), partType, out var partEnt) &&
                partEnt is { Valid: true } partEntValid)
                RaiseLocalEvent(partEntValid, ev);
        }

        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<SiliconComponentsComponent> ent, T args) where T : ISiliconPartRelayEvent
    {
        if (args.Parts == PartType.NONE)
            return;

        var ev = new PartRelayedEvent<T>(args, ent.Owner);

        if (args.Parts != PartType.ALL)
        {
            if (TryGetPart(ent.AsNullable(), args.Parts, out var partEnt) &&
                partEnt is { Valid: true } partEntValid)
                RaiseLocalEvent(partEntValid, ev);

            args = ev.Args;

            return;
        }

        foreach (var partType in ent.Comp.Parts.Keys)
        {
            if (TryGetPart(ent.AsNullable(), partType, out var partEnt) &&
                partEnt is { Valid: true } partEntValid)
                RaiseLocalEvent(partEntValid, ev);
        }

        args = ev.Args;
    }

    public bool TryGetOperational(Entity<SiliconComponentsComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        foreach (var crucialPart in ent.Comp.PartsRequiredToOperate)
        {
            if (!TryGetPart(ent, crucialPart, out var partUid) ||
                !TryComp<SiliconPartComponent>(partUid, out var partComp) ||
                !partComp.Active)
                return false;
        }

        return true;
    }

    public void SetOperational(Entity<SiliconComponentsComponent?> ent, bool operational)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (operational)
        {
            RemComp<StunnedComponent>(ent);
            RemComp<KnockedDownComponent>(ent);
            return;
        }

        EnsureComp<StunnedComponent>(ent);
        EnsureComp<KnockedDownComponent>(ent);
    }

    private void OnGetVerbs(Entity<SiliconComponentsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != ent.Owner)
            return;

        if (TryComp<LockComponent>(ent.Owner, out var lockComp) &&
            lockComp.Locked)
            return;

        if (!TryComp<WiresPanelComponent>(ent.Owner, out var panelComp))
            return;

        var user = args.User;

        var messageId = !panelComp.Open ? BuiAltVerbOpen : BuiAltVerbClose;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TogglePanel(ent, !panelComp.Open),
            Text = Loc.GetString(messageId),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    private void OnEyeDamage(Entity<SiliconComponentsComponent> ent, ref EyeDamageChangedEvent args)
    {
        if (!TryGetPart(ent.AsNullable(), PartType.Optics, out var opticsEnt) ||
            opticsEnt is not { Valid: true } opticsEntValidated)
            return;

        if (!TryComp<DamabeableSiliconPartComponent>(opticsEntValidated, out var damageablePartComp))
            return;

        if (!TryComp<BlindableComponent>(opticsEntValidated, out var blindableComp))
            return;

        var requiredDamage = damageablePartComp.MaxDamageToRemainFunctional * (blindableComp.EyeDamage / blindableComp.MaxDamage);

        if (_damageableSystem.GetTotalDamage(opticsEntValidated) < requiredDamage)
            _damageableSystem.TryChangeDamage(opticsEntValidated, new DamageSpecifier(_prototype.Index(EyeDamageType), requiredDamage - _damageableSystem.GetTotalDamage(opticsEntValidated)));
    }

    private void TogglePanel(Entity<SiliconComponentsComponent> ent, bool open)
    {
        if (!TryComp<WiresPanelComponent>(ent.Owner, out var panelComp))
            return;

        _wires.TogglePanel(ent, panelComp, !panelComp.Open);

        if (panelComp.Open)
        {
            _audio.PlayPredicted(ent.Comp.UnlockSound, ent.Owner, ent.Owner);
            return;
        }

        _audio.PlayPredicted(ent.Comp.LockSound, ent.Owner, ent.Owner);
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallSiliconPartEvent : SimpleDoAfterEvent
{
    public PartType Slot;

    public InstallSiliconPartEvent(PartType slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveSiliconPartEvent : SimpleDoAfterEvent
{
    public PartType Slot;

    public RemoveSiliconPartEvent(PartType slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallSiliconModuleEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveSiliconModuleEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public record struct ComponentGotInsertedIntoUser(EntityUid Owner)
{
}

[ByRefEvent]
public record struct ComponentGotRemovedFromUser(EntityUid Owner)
{
}

[ByRefEvent]
public record struct ComponentInsertedIntoUser(EntityUid Part)
{
}

[ByRefEvent]
public record struct ComponentRemovedFromUser(EntityUid Part)
{
}

public sealed class PartRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Owner;

    public PartRelayedEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Owner = owner;
    }
}

public interface ISiliconPartRelayEvent
{
    public PartType Parts { get; }
}
