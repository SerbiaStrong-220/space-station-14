using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.SS220.Hookah.Components;
using Content.Shared.SS220.HookahElectric;
using Content.Shared.SS220.HookahElectric.Components;
using Content.Shared.Stacks;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.HookahElectric;

public sealed class HookahElectricSystem : EntitySystem
{
    private static readonly LocId HookahHosesFull = "hookah-electric-hoses-full";
    private static readonly LocId HookahNoBattery = "hookah-electric-no-battery";
    private static readonly LocId HookahLowBattery = "hookah-electric-low-battery";
    private static readonly LocId HookahTurnedOn = "hookah-electric-turned-on";
    private static readonly LocId HookahTurnedOff = "hookah-electric-turned-off";
    private static readonly LocId HookahBatteryOut = "hookah-electric-battery-out";
    private static readonly LocId HookahNotOn = "hookah-electric-not-on";
    private static readonly LocId HookahSolutionEmpty = "hookah-electric-solution-empty";
    private static readonly LocId HookahDragStart = "hookah-electric-drag-start";
    private static readonly LocId HookahSmoke = "hookah-electric-smoke";
    private static readonly LocId HookahHoseTooFar = "hookah-electric-hose-too-far";
    private static readonly LocId HookahTobaccoSlotFull = "hookah-tobacco-slot-full";
    private static readonly LocId HookahTobaccoInserted = "hookah-tobacco-inserted";
    private static readonly LocId HookahTobaccoEmpty = "hookah-tobacco-empty";
    private static readonly LocId HookahExamineTobacco = "hookah-examine-tobacco";
    private static readonly LocId HookahVerbTurnOn = "hookah-electric-verb-turn-on";
    private static readonly LocId HookahVerbTurnOff = "hookah-electric-verb-turn-off";
    private static readonly LocId HookahVerbEjectCell = "hookah-electric-verb-eject-cell";

    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HookahElectricComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HookahElectricComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HookahElectricComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<HookahElectricComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HookahElectricComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HookahElectricComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HookahElectricComponent, GetVerbsEvent<Verb>>(OnGetEjectVerb);
        SubscribeLocalEvent<HookahElectricComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
        SubscribeLocalEvent<HookahElectricComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        SubscribeLocalEvent<HookahElectricHoseComponent, UseInHandEvent>(OnUseHose);
        SubscribeLocalEvent<HookahElectricHoseComponent, HookahElectricSmokeDoAfterEvent>(OnSmokeDoAfter);
        SubscribeLocalEvent<HookahElectricHoseComponent, ComponentShutdown>(OnHoseShutdown);
        SubscribeLocalEvent<HookahElectricHoseComponent, DroppedEvent>(OnHoseDropped);
        SubscribeLocalEvent<HookahElectricHoseComponent, EntParentChangedMessage>(OnHoseParentChanged);
    }

    public override void Update(float frameTime)
    {
        UpdateHoses(frameTime);
    }

    private void OnInit(Entity<HookahElectricComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnShutdown(Entity<HookahElectricComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.LeftHose is { } left && !TerminatingOrDeleted(left))
            QueueDel(left);

        if (ent.Comp.RightHose is { } right && !TerminatingOrDeleted(right))
            QueueDel(right);
    }

    private void OnInteractHand(Entity<HookahElectricComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        HookahElectricHoseSide? side = null;
        if (ent.Comp.LeftHose == null || TerminatingOrDeleted(ent.Comp.LeftHose.Value))
            side = HookahElectricHoseSide.Left;
        else if (ent.Comp.RightHose == null || TerminatingOrDeleted(ent.Comp.RightHose.Value))
            side = HookahElectricHoseSide.Right;

        if (side == null)
        {
            _popup.PopupEntity(Loc.GetString(HookahHosesFull), ent, args.User);
            args.Handled = true;
            return;
        }

        _transform.SetLocalRotation(ent, Angle.Zero);

        var hose = Spawn(ent.Comp.HosePrototype, _transform.GetMapCoordinates(ent));
        var hoseComp = EnsureComp<HookahElectricHoseComponent>(hose);
        hoseComp.HookahUid = ent;
        hoseComp.Side = side.Value;

        if (side == HookahElectricHoseSide.Left)
            ent.Comp.LeftHose = hose;
        else
            ent.Comp.RightHose = hose;
        Dirty(ent);

        var visuals = EnsureComp<JointVisualsComponent>(hose);
        visuals.Sprite = ent.Comp.RopeSprite;
        visuals.Target = ent;
        visuals.OffsetB = side == HookahElectricHoseSide.Left
            ? new Vector2(-0.15f, 0.1f)
            : new Vector2(0.15f, 0.1f);
        Dirty(hose, visuals);

        _hands.TryPickupAnyHand(args.User, hose);
        RefreshHose((hose, hoseComp));
        UpdateAppearance(ent);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<HookahElectricComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<SmokingFuelComponent>(ent, out var fuel) && IsTobacco(args.Used, fuel))
        {
            InsertTobacco(ent, ref args, fuel);
            return;
        }
    }

    private void InsertTobacco(Entity<HookahElectricComponent> ent, ref InteractUsingEvent args, SmokingFuelComponent fuel)
    {
        if (fuel.TobaccoSlot.Item != null)
        {
            _popup.PopupEntity(Loc.GetString(HookahTobaccoSlotFull), ent, args.User);
            args.Handled = true;
            return;
        }

        if (_itemSlots.TryInsert(ent, fuel.TobaccoSlot, args.Used, args.User))
        {
            _itemSlots.SetLock(ent, fuel.TobaccoSlot, true);
            _popup.PopupEntity(Loc.GetString(HookahTobaccoInserted), ent, args.User);
        }

        args.Handled = true;
    }

    private void OnGetVerbs(Entity<HookahElectricComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var on = ent.Comp.IsOn;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString(on ? HookahVerbTurnOff : HookahVerbTurnOn),
            Act = () => TryToggle(ent, user),
            Priority = 1,
        };
        args.Verbs.Add(verb);
    }

    private void OnGetEjectVerb(Entity<HookahElectricComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_itemSlots.TryGetSlot(ent, "cell_slot", out var slot) || slot.Item == null)
            return;

        var user = args.User;
        var verb = new Verb
        {
            Text = Loc.GetString(HookahVerbEjectCell),
            Category = VerbCategory.Eject,
            Act = () => _itemSlots.TryEjectToHands(ent, slot, user, excludeUserAudio: true),
        };
        args.Verbs.Add(verb);
    }

    private void TryToggle(Entity<HookahElectricComponent> ent, EntityUid user)
    {
        if (ent.Comp.IsOn)
        {
            SetOn(ent, false);
            _popup.PopupEntity(Loc.GetString(HookahTurnedOff), ent, user);
            return;
        }

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString(HookahNoBattery), ent, user);
            return;
        }

        if (!_powerCell.HasDrawCharge(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString(HookahLowBattery), ent, user);
            return;
        }

        SetOn(ent, true);
        _popup.PopupEntity(Loc.GetString(HookahTurnedOn), ent, user);
    }

    private void SetOn(Entity<HookahElectricComponent> ent, bool on)
    {
        if (ent.Comp.IsOn == on)
            return;

        ent.Comp.IsOn = on;
        Dirty(ent);

        _powerCell.SetDrawEnabled(ent.Owner, on);
        UpdateAppearance(ent);
        _audio.PlayPvs(on ? ent.Comp.ToggleOnSound : ent.Comp.ToggleOffSound, ent);
    }

    private void OnPowerCellEmpty(Entity<HookahElectricComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (!ent.Comp.IsOn)
            return;

        SetOn(ent, false);
        BatteryOutPopup(ent);
    }

    private void OnPowerCellChanged(Entity<HookahElectricComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected || !ent.Comp.IsOn)
            return;

        SetOn(ent, false);
        BatteryOutPopup(ent);
    }

    private void BatteryOutPopup(Entity<HookahElectricComponent> ent)
    {
        foreach (var hose in new[] { ent.Comp.LeftHose, ent.Comp.RightHose })
        {
            if (hose is not { } h ||
                TerminatingOrDeleted(h) ||
                !_container.TryGetContainingContainer(h, out var container) ||
                !HasComp<HandsComponent>(container.Owner))
                continue;

            _popup.PopupEntity(Loc.GetString(HookahBatteryOut), container.Owner, container.Owner);
        }
    }

    private void OnUseHose(Entity<HookahElectricHoseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<HookahElectricComponent>(ent.Comp.HookahUid, out var hookah))
            return;

        if (!CheckSmoke((ent.Comp.HookahUid, hookah), ent, args.User))
        {
            args.Handled = true;
            return;
        }

        _audio.PlayPvs(hookah.UseSound, args.User);
        _useDelay.TryResetDelay(ent.Owner);

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.User,
            hookah.DragDelay,
            new HookahElectricSmokeDoAfterEvent(),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
            BlockDuplicate = true,
        });

        _popup.PopupEntity(Loc.GetString(HookahDragStart), ent, args.User);
        args.Handled = true;
    }

    private void OnSmokeDoAfter(Entity<HookahElectricHoseComponent> ent, ref HookahElectricSmokeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<HookahElectricComponent>(ent.Comp.HookahUid, out var hookah))
            return;

        if (!CheckSmoke((ent.Comp.HookahUid, hookah), ent, args.User))
        {
            args.Handled = true;
            return;
        }

        if (!TakeTobacco((ent.Comp.HookahUid, hookah), ent, args.User))
        {
            args.Handled = true;
            return;
        }

        if (!_solutions.TryGetSolution(ent.Comp.HookahUid, hookah.SolutionName, out var solutionEnt, out _))
        {
            args.Handled = true;
            return;
        }

        var inhaled = _solutions.SplitSolution(solutionEnt.Value, FixedPoint2.New(hookah.InhaleAmount));

        if (TryComp<BloodstreamComponent>(args.User, out var bloodstream))
        {
            _reactive.DoEntityReaction(args.User, inhaled, ReactionMethod.Ingestion);
            _bloodstream.TryAddToBloodstream((args.User, bloodstream), inhaled);
        }

        Exhale(args.User, hookah);

        _popup.PopupEntity(Loc.GetString(HookahSmoke), ent, args.User);
        args.Handled = true;
    }

    private void Exhale(EntityUid user, HookahElectricComponent hookah)
    {
        var environment = _atmos.GetContainingMixture(user, true, true);
        if (environment == null)
            return;

        var gas = new GasMixture(1)
        {
            Temperature = Atmospherics.T20C,
        };

        gas.SetMoles(hookah.ExhaleGasType, hookah.ExhaleMoles);
        _atmos.Merge(environment, gas);
    }

    private bool CheckSmoke(Entity<HookahElectricComponent> hookah, EntityUid hose, EntityUid user)
    {
        if (!hookah.Comp.IsOn)
        {
            _popup.PopupEntity(Loc.GetString(HookahNotOn), hose, user);
            return false;
        }

        if (!_solutions.TryGetSolution(hookah.Owner, hookah.Comp.SolutionName, out _, out var solution))
            return false;

        if (solution.Volume > FixedPoint2.Zero)
            return true;

        _popup.PopupEntity(Loc.GetString(HookahSolutionEmpty), hose, user);
        return false;
    }

    private bool TakeTobacco(Entity<HookahElectricComponent> hookah, EntityUid hose, EntityUid user)
    {
        if (!TryComp<SmokingFuelComponent>(hookah, out var fuel))
            return true;

        if (fuel.TobaccoPuffs > 0)
        {
            fuel.TobaccoPuffs--;
            return true;
        }

        if (fuel.TobaccoSlot.Item is not { } tobacco || !IsTobacco(tobacco, fuel))
        {
            _popup.PopupEntity(Loc.GetString(HookahTobaccoEmpty), hose, user);
            return false;
        }

        _itemSlots.SetLock(hookah, fuel.TobaccoSlot, false);

        if (TryComp<StackComponent>(tobacco, out var stack) && stack.Count > 1)
        {
            _stack.TryUse(tobacco, 1);
            _itemSlots.SetLock(hookah, fuel.TobaccoSlot, true);
        }
        else
        {
            if (fuel.TobaccoSlot.ContainerSlot != null)
                _container.Remove(tobacco, fuel.TobaccoSlot.ContainerSlot);

            QueueDel(tobacco);
        }

        fuel.TobaccoPuffs = fuel.PuffsPerPack - 1;
        return true;
    }

    private bool IsTobacco(EntityUid uid, SmokingFuelComponent fuel)
    {
        return MetaData(uid).EntityPrototype?.ID is { } id && id == fuel.TobaccoId;
    }

    private void OnHoseShutdown(Entity<HookahElectricHoseComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HookahElectricComponent>(ent.Comp.HookahUid, out var hookah))
            return;

        var changed = false;
        if (hookah.LeftHose == ent.Owner)
        {
            hookah.LeftHose = null;
            changed = true;
        }
        if (hookah.RightHose == ent.Owner)
        {
            hookah.RightHose = null;
            changed = true;
        }

        if (!changed)
            return;

        Dirty(ent.Comp.HookahUid, hookah);
        UpdateAppearance((ent.Comp.HookahUid, hookah));
    }

    private void OnHoseDropped(Entity<HookahElectricHoseComponent> ent, ref DroppedEvent args)
    {
        RemComp<ActiveHookahElectricHoseComponent>(ent);
        RemComp<JointVisualsComponent>(ent);
        QueueDel(ent);
    }

    private void OnHoseParentChanged(Entity<HookahElectricHoseComponent> ent, ref EntParentChangedMessage args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RefreshHose(ent);

        var parent = args.Transform.ParentUid;
        if (!parent.IsValid() ||
            HasComp<MapComponent>(parent) ||
            HasComp<MapGridComponent>(parent) ||
            HasComp<HandsComponent>(parent))
            return;

        QueueDel(ent);
    }

    private void RefreshHose(Entity<HookahElectricHoseComponent> ent)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<HandsComponent>(container.Owner))
        {
            EnsureComp<ActiveHookahElectricHoseComponent>(ent);
            return;
        }

        RemComp<ActiveHookahElectricHoseComponent>(ent);
    }

    private void UpdateHoses(float frameTime)
    {
        var query = EntityQueryEnumerator<HookahElectricHoseComponent, ActiveHookahElectricHoseComponent>();
        while (query.MoveNext(out var uid, out var hose, out var active))
        {
            active.Accum += TimeSpan.FromSeconds(frameTime);
            if (active.Accum < hose.CheckInterval)
                continue;

            active.Accum = TimeSpan.Zero;

            if (!_container.TryGetContainingContainer(uid, out var container) ||
                !TryComp<HandsComponent>(container.Owner, out var hands))
            {
                RemComp<ActiveHookahElectricHoseComponent>(uid);
                continue;
            }

            if (!TryComp<HookahElectricComponent>(hose.HookahUid, out _))
            {
                QueueDel(uid);
                continue;
            }

            var hosePos = _transform.GetWorldPosition(uid);
            var hookahPos = _transform.GetWorldPosition(hose.HookahUid);

            if ((hosePos - hookahPos).LengthSquared() <= hose.MaxDistance * hose.MaxDistance)
                continue;

            RemComp<JointVisualsComponent>(uid);
            _hands.TryDrop((container.Owner, hands), uid);
            _popup.PopupEntity(Loc.GetString(HookahHoseTooFar), container.Owner, container.Owner);
        }
    }

    private void UpdateAppearance(Entity<HookahElectricComponent> ent)
    {
        _appearance.SetData(ent, HookahElectricVisuals.Enabled, ent.Comp.IsOn);
        _appearance.SetData(ent, HookahElectricVisuals.LeftHose,
            ent.Comp.LeftHose == null || TerminatingOrDeleted(ent.Comp.LeftHose.Value));
        _appearance.SetData(ent, HookahElectricVisuals.RightHose,
            ent.Comp.RightHose == null || TerminatingOrDeleted(ent.Comp.RightHose.Value));
    }

    private void OnExamined(Entity<HookahElectricComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<SmokingFuelComponent>(ent, out var fuel))
            return;

        if (fuel.TobaccoPuffs > 0 || fuel.TobaccoSlot.Item != null)
            args.PushText(Loc.GetString(HookahExamineTobacco, ("puffs", fuel.TobaccoPuffs)));
    }
}
