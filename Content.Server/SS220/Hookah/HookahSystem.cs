using System;
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
using Content.Shared.SS220.Hookah;
using Content.Shared.SS220.Hookah.Components;
using Content.Shared.Stacks;
using Content.Shared.Temperature;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Hookah;

public sealed class HookahSystem : EntitySystem
{
    private static readonly LocId HookahHoseAlreadyConnected = "hookah-hose-already-connected";
    private static readonly LocId HookahAlreadyLit = "hookah-already-lit";
    private static readonly LocId HookahNoCoal = "hookah-no-coal";
    private static readonly LocId HookahLit = "hookah-lit";
    private static readonly LocId HookahCoalSlotFull = "hookah-coal-slot-full";
    private static readonly LocId HookahCoalInserted = "hookah-coal-inserted";
    private static readonly LocId HookahTobaccoSlotFull = "hookah-tobacco-slot-full";
    private static readonly LocId HookahTobaccoInserted = "hookah-tobacco-inserted";
    private static readonly LocId HookahDragStart = "hookah-drag-start";
    private static readonly LocId HookahSmoke = "hookah-smoke";
    private static readonly LocId HookahNotLit = "hookah-not-lit";
    private static readonly LocId HookahSolutionEmpty = "hookah-solution-empty";
    private static readonly LocId HookahTobaccoEmpty = "hookah-tobacco-empty";
    private static readonly LocId HookahHoseTooFar = "hookah-hose-too-far";
    private static readonly LocId HookahCoalOut = "hookah-coal-out";
    private static readonly LocId HookahExamineTobacco = "hookah-examine-tobacco";
    private static readonly LocId HookahExamineCoal = "hookah-examine-coal";

    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
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

        SubscribeLocalEvent<HookahComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HookahComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HookahComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<HookahComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HookahComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<HookahHoseComponent, UseInHandEvent>(OnUseHose);
        SubscribeLocalEvent<HookahHoseComponent, HookahSmokeDoAfterEvent>(OnSmokeDoAfter);
        SubscribeLocalEvent<HookahHoseComponent, ComponentShutdown>(OnHoseShutdown);
        SubscribeLocalEvent<HookahHoseComponent, DroppedEvent>(OnHoseDropped);
        SubscribeLocalEvent<HookahHoseComponent, EntParentChangedMessage>(OnHoseParentChanged);

        SubscribeLocalEvent<SmokingFuelComponent, ComponentInit>(OnFuelInit);
        SubscribeLocalEvent<SmokingFuelComponent, ComponentShutdown>(OnFuelShutdown);
    }

    public override void Update(float frameTime)
    {
        UpdateHoses(frameTime);
        UpdateCoal(frameTime);
    }

    private void OnFuelInit(Entity<SmokingFuelComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent, SmokingFuelComponent.TobaccoSlotId, ent.Comp.TobaccoSlot);
    }

    private void OnFuelShutdown(Entity<SmokingFuelComponent> ent, ref ComponentShutdown args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.TobaccoSlot);
    }

    private void OnInit(Entity<HookahComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent, HookahComponent.CoalSlotId, ent.Comp.CoalSlot);

        if (ent.Comp.IsLit)
            EnsureComp<ActiveHookahComponent>(ent);

        UpdateAppearance(ent);
    }

    private void OnShutdown(Entity<HookahComponent> ent, ref ComponentShutdown args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.CoalSlot);

        if (ent.Comp.ConnectedHose is { } hose && !TerminatingOrDeleted(hose))
            QueueDel(hose);
    }

    private void OnInteractHand(Entity<HookahComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ConnectedHose is { } existing && !TerminatingOrDeleted(existing))
        {
            _popup.PopupEntity(Loc.GetString(HookahHoseAlreadyConnected), ent, args.User);
            args.Handled = true;
            return;
        }

        var hose = Spawn(ent.Comp.HosePrototype, _transform.GetMapCoordinates(ent));
        var hoseComp = EnsureComp<HookahHoseComponent>(hose);
        hoseComp.HookahUid = ent;

        ent.Comp.ConnectedHose = hose;
        Dirty(ent);

        var visuals = EnsureComp<JointVisualsComponent>(hose);
        visuals.Sprite = ent.Comp.RopeSprite;
        visuals.Target = ent;
        visuals.OffsetB = new Vector2(0.15f, 0f);
        Dirty(hose, visuals);

        _hands.TryPickupAnyHand(args.User, hose);
        RefreshHose((hose, hoseComp));
        UpdateAppearance(ent);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<HookahComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<HookahCoalComponent>(args.Used))
        {
            InsertCoal(ent, ref args);
            return;
        }

        if (TryComp<SmokingFuelComponent>(ent, out var fuel) && IsTobacco(args.Used, fuel))
        {
            InsertTobacco(ent, ref args, fuel);
            return;
        }

        var hot = new IsHotEvent();
        RaiseLocalEvent(args.Used, hot);

        if (!hot.IsHot)
            return;

        if (ent.Comp.IsLit)
        {
            _popup.PopupEntity(Loc.GetString(HookahAlreadyLit), ent, args.User);
            args.Handled = true;
            return;
        }

        if (ent.Comp.CoalSlot.Item == null)
        {
            _popup.PopupEntity(Loc.GetString(HookahNoCoal), ent, args.User);
            args.Handled = true;
            return;
        }

        SetLit(ent, true);
        _popup.PopupEntity(Loc.GetString(HookahLit), ent, args.User);
        args.Handled = true;
    }

    private void InsertCoal(Entity<HookahComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.CoalSlot.Item != null)
        {
            _popup.PopupEntity(Loc.GetString(HookahCoalSlotFull), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!_itemSlots.TryInsert(ent, ent.Comp.CoalSlot, args.Used, args.User))
            return;

        _itemSlots.SetLock(ent, ent.Comp.CoalSlot, true);
        _popup.PopupEntity(Loc.GetString(HookahCoalInserted), ent, args.User);
        UpdateAppearance(ent);
        args.Handled = true;
    }

    private void InsertTobacco(Entity<HookahComponent> ent, ref InteractUsingEvent args, SmokingFuelComponent fuel)
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

    private void OnUseHose(Entity<HookahHoseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<HookahComponent>(ent.Comp.HookahUid, out var hookah))
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
            new HookahSmokeDoAfterEvent(),
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

    private void OnSmokeDoAfter(Entity<HookahHoseComponent> ent, ref HookahSmokeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<HookahComponent>(ent.Comp.HookahUid, out var hookah))
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

        if (!_solutions.TryGetSolution(ent.Comp.HookahUid, hookah.SolutionName, out var solutionEnt, out var solution))
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

    private bool CheckSmoke(Entity<HookahComponent> hookah, EntityUid hose, EntityUid user)
    {
        if (!hookah.Comp.IsLit)
        {
            _popup.PopupEntity(Loc.GetString(HookahNotLit), hose, user);
            return false;
        }

        if (!_solutions.TryGetSolution(hookah.Owner, hookah.Comp.SolutionName, out _, out var solution))
            return false;

        if (solution.Volume > FixedPoint2.Zero)
            return true;

        _popup.PopupEntity(Loc.GetString(HookahSolutionEmpty), hose, user);
        return false;
    }

    private bool TakeTobacco(Entity<HookahComponent> hookah, EntityUid hose, EntityUid user)
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
            _stack.TryUse((tobacco, stack), 1);
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

    private void Exhale(EntityUid user, HookahComponent hookah)
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

    private void OnHoseShutdown(Entity<HookahHoseComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HookahComponent>(ent.Comp.HookahUid, out var hookah) ||
            hookah.ConnectedHose != ent.Owner)
            return;

        hookah.ConnectedHose = null;
        Dirty(ent.Comp.HookahUid, hookah);
        UpdateAppearance((ent.Comp.HookahUid, hookah));
    }

    private void OnHoseDropped(Entity<HookahHoseComponent> ent, ref DroppedEvent args)
    {
        RemComp<ActiveHookahHoseComponent>(ent);
        RemComp<JointVisualsComponent>(ent);
        QueueDel(ent);
    }

    private void OnHoseParentChanged(Entity<HookahHoseComponent> ent, ref EntParentChangedMessage args)
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

    private void RefreshHose(Entity<HookahHoseComponent> ent)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<HandsComponent>(container.Owner))
        {
            EnsureComp<ActiveHookahHoseComponent>(ent);
            return;
        }

        RemComp<ActiveHookahHoseComponent>(ent);
    }

    private void UpdateHoses(float frameTime)
    {
        var query = EntityQueryEnumerator<HookahHoseComponent, ActiveHookahHoseComponent>();
        while (query.MoveNext(out var uid, out var hose, out var active))
        {
            active.Accum += TimeSpan.FromSeconds(frameTime);
            if (active.Accum < hose.CheckInterval)
                continue;

            active.Accum = TimeSpan.Zero;

            if (!_container.TryGetContainingContainer(uid, out var container) ||
                !TryComp<HandsComponent>(container.Owner, out var hands))
            {
                RemComp<ActiveHookahHoseComponent>(uid);
                continue;
            }

            if (!TryComp<HookahComponent>(hose.HookahUid, out _))
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

    private void UpdateCoal(float frameTime)
    {
        var query = EntityQueryEnumerator<HookahComponent, ActiveHookahComponent>();
        while (query.MoveNext(out var uid, out var hookah, out _))
        {
            if (hookah.CoalSlot.Item is not { } coalUid)
            {
                SetLit((uid, hookah), false);
                continue;
            }

            if (!TryComp<HookahCoalComponent>(coalUid, out var coal))
            {
                Log.Warning($"{ToPrettyString(uid)} has non-coal entity {ToPrettyString(coalUid)} in its coal slot.");
                SetLit((uid, hookah), false);
                continue;
            }

            coal.FuelLeft -= coal.FuelDrainIdle * frameTime;

            if (TryComp<SmokingFuelComponent>(uid, out var fuel))
            {
                fuel.CoalTime = coal.FuelDrainIdle > 0f
                    ? MathF.Max(0f, coal.FuelLeft / coal.FuelDrainIdle)
                    : 0f;
            }

            if (coal.FuelLeft > 0f)
                continue;

            SetLit((uid, hookah), false);
            CoalOutPopup((uid, hookah));

            _itemSlots.SetLock(uid, hookah.CoalSlot, false);
            _itemSlots.TryEject(uid, hookah.CoalSlot, null, out _);
            QueueDel(coalUid);
        }
    }

    private void CoalOutPopup(Entity<HookahComponent> ent)
    {
        if (ent.Comp.ConnectedHose is not { } hose ||
            TerminatingOrDeleted(hose) ||
            !_container.TryGetContainingContainer(hose, out var container) ||
            !HasComp<HandsComponent>(container.Owner))
            return;

        _popup.PopupEntity(Loc.GetString(HookahCoalOut), container.Owner, container.Owner);
    }

    private void SetLit(Entity<HookahComponent> ent, bool lit)
    {
        if (ent.Comp.IsLit == lit)
            return;

        ent.Comp.IsLit = lit;
        Dirty(ent);

        if (lit)
            EnsureComp<ActiveHookahComponent>(ent);
        else
            RemComp<ActiveHookahComponent>(ent);

        _itemSlots.SetLock(ent, ent.Comp.CoalSlot, lit);
        UpdateAppearance(ent);
        _audio.PlayPvs(lit ? ent.Comp.LightSound : ent.Comp.ExtinguishSound, ent);
    }

    private void UpdateAppearance(Entity<HookahComponent> ent)
    {
        var hoseOut = ent.Comp.ConnectedHose != null;
        var state = ent.Comp.IsLit
            ? hoseOut ? HookahVisualState.CoalLitNoHose : HookahVisualState.CoalLit
            : ent.Comp.CoalSlot.Item != null
                ? hoseOut ? HookahVisualState.CoalUnlitNoHose : HookahVisualState.CoalUnlit
                : hoseOut ? HookahVisualState.UnlitNoHose : HookahVisualState.Unlit;

        _appearance.SetData(ent, HookahVisuals.State, state);
    }

    private void OnExamined(Entity<HookahComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<SmokingFuelComponent>(ent, out var fuel))
            return;

        if (fuel.TobaccoPuffs > 0 || fuel.TobaccoSlot.Item != null)
            args.PushText(Loc.GetString(HookahExamineTobacco, ("puffs", fuel.TobaccoPuffs)));

        if (ent.Comp.IsLit && fuel.CoalTime > 0f)
            args.PushText(Loc.GetString(HookahExamineCoal, ("seconds", (int) fuel.CoalTime)));
    }
}
