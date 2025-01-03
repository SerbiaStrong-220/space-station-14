// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Construction;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.Storage.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.SS220.SupaKitchen;
public sealed class CookingMachineSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly CookingInstrumentSystem _cookingInstrument = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CookingMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CookingMachineComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
        SubscribeLocalEvent<CookingMachineComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<CookingMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CookingMachineComponent, BreakageEventArgs>(OnBreak);
        //SubscribeLocalEvent<CookingMachineComponent, RefreshPartsEvent>(OnRefreshParts);
        //SubscribeLocalEvent<CookingMachineComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<CookingMachineComponent, StorageAfterOpenEvent>(OnStorageOpen);
        SubscribeLocalEvent<CookingMachineComponent, StorageAfterCloseEvent>(OnStorageClosed);

        // UI event listeners
        SubscribeLocalEvent<CookingMachineComponent, CookingMachineStartCookMessage>((u, c, m) => StartCooking(u, c, m.Actor));
        SubscribeLocalEvent<CookingMachineComponent, CookingMachineEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<CookingMachineComponent, CookingMachineEjectSolidIndexedMessage>(OnEjectIndex);
        SubscribeLocalEvent<CookingMachineComponent, CookingMachineSelectCookTimeMessage>(OnSelectTime);
    }

    private void OnStorageOpen(EntityUid uid, CookingMachineComponent component, StorageAfterOpenEvent args)
    {
        StopCooking(uid, component);
        UpdateUserInterfaceState(uid, component);
    }

    private void OnStorageClosed(EntityUid uid, CookingMachineComponent component, StorageAfterCloseEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    private void OnInit(EntityUid uid, CookingMachineComponent component, ComponentInit ags)
    {
        if (component.UseEntityStorage)
            component.Storage = _container.EnsureContainer<Container>(uid, EntityStorageSystem.ContainerName);
        else
            component.Storage = _container.EnsureContainer<Container>(uid, "cooking_machine_entity_container");

    }

    private void OnPowerChanged(EntityUid uid, CookingMachineComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(uid, CookingMachineVisualState.Idle, component);
            StopCooking(uid, component);
            if (!component.UseEntityStorage)
                _sharedContainer.EmptyContainer(component.Storage);
        }
        UpdateUserInterfaceState(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, CookingMachineComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (component.UseEntityStorage)
            return;

        if (!(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
        {
            _popupSystem.PopupEntity(
                Loc.GetString("cooking-machine-component-interact-using-no-power", ("machine", MetaData(uid).EntityName)),
                uid,
                args.User);
            return;
        }

        if (component.Broken)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("cooking-machine-component-interact-using-broken", ("machine", MetaData(uid).EntityName)),
                uid,
                args.User);
            return;
        }

        if (!HasComp<ItemComponent>(args.Used))
        {
            _popupSystem.PopupEntity(Loc.GetString("cooking-machine-component-interact-using-transfer-fail"),
                uid,
                args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, component.Storage);
        UpdateUserInterfaceState(uid, component);
    }

    private void OnSolutionChange(EntityUid uid, CookingMachineComponent component, SolutionChangedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    private void OnBreak(EntityUid uid, CookingMachineComponent component, BreakageEventArgs args)
    {
        component.Broken = true;
        StopCooking(uid, component);
        SetAppearance(uid, CookingMachineVisualState.Broken, component);

        if (component.UseEntityStorage)
            _entityStorage.OpenStorage(uid);
        else
            _sharedContainer.EmptyContainer(component.Storage);

        UpdateUserInterfaceState(uid, component);
    }

    //private void OnRefreshParts(EntityUid uid, CookingMachineComponent component, RefreshPartsEvent args)
    //{
    //    var cookRating = args.PartRatings[component.MachinePartCookTimeMultiplier];
    //    component.CookTimeMultiplier = MathF.Pow(component.CookTimeScalingConstant, cookRating - 1);
    //}

    //private void OnUpgradeExamine(EntityUid uid, CookingMachineComponent component, UpgradeExamineEvent args)
    //{
    //    args.AddPercentageUpgrade("cooking-machine-component-upgrade-cook-time", component.CookTimeMultiplier);
    //}

    #region ui_messages
    private void OnEjectMessage(EntityUid uid, CookingMachineComponent component, CookingMachineEjectMessage args)
    {
        if (!HasContents(component) || component.Active)
            return;

        if (!component.UseEntityStorage)
            _sharedContainer.EmptyContainer(component.Storage);

        _audio.PlayPvs(component.ClickSound, uid, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(uid, component);
    }

    private void OnEjectIndex(EntityUid uid, CookingMachineComponent component, CookingMachineEjectSolidIndexedMessage args)
    {
        if (!HasContents(component) || component.Active)
            return;

        _container.Remove(GetEntity(args.EntityID), component.Storage);
        UpdateUserInterfaceState(uid, component);
    }

    private void OnSelectTime(EntityUid uid, CookingMachineComponent component, CookingMachineSelectCookTimeMessage args)
    {
        if (!HasContents(component) || component.Active || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > component.MaxCookingTimer)
            return;

        component.CurrentCookTimeButtonIndex = args.ButtonIndex;
        component.CookingTimer = args.NewCookTime;
        _audio.PlayPvs(component.ClickSound, uid, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(uid, component);
    }
    #endregion

    public static bool HasContents(CookingMachineComponent component)
    {
        return component.Storage.ContainedEntities.Any();
    }

    public void SetAppearance(EntityUid uid, CookingMachineVisualState state, CookingMachineComponent component)
    {
        var display = component.Broken ? CookingMachineVisualState.Broken : state;
        _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display);
    }

    public void UpdateUserInterfaceState(EntityUid uid, CookingMachineComponent component)
    {
        _userInterface.SetUiState(uid, CookingMachineUiKey.Key, new CookingMachineUpdateUserInterfaceState(
            GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
            component.Active,
            component.CurrentCookTimeButtonIndex,
            component.CookingTimer,
            component.UseEntityStorage
        ));
    }

    public void Break(EntityUid uid, CookingMachineComponent component)
    {
        component.Broken = true;
        SetAppearance(uid, CookingMachineVisualState.Broken, component);
        _audio.PlayPvs(component.ItemBreakSound, uid);
    }

    public void StartCooking(EntityUid uid, CookingMachineComponent component, EntityUid? whoStarted = null)
    {
        if (!TryComp<CookingInstrumentComponent>(uid, out var cookingInstrument))
            return;

        if (!HasContents(component) || component.Active)
            return;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();

        foreach (var item in component.Storage.ContainedEntities.ToList())
        {
            var ev = new ProcessedInCookingMachineEvent(uid, component, item, whoStarted);
            RaiseLocalEvent(uid, ev);

            if (ev.Handled)
            {
                UpdateUserInterfaceState(uid, component);
                return;
            }

            var metaData = MetaData(item); //this still begs for cooking refactor
            if (metaData.EntityPrototype == null)
                continue;

            if (solidsDict.ContainsKey(metaData.EntityPrototype.ID))
                solidsDict[metaData.EntityPrototype.ID]++;
            else
                solidsDict.Add(metaData.EntityPrototype.ID, 1);

            if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                continue;

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
            {
                var solution = soln.Comp.Solution;
                {
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        if (reagentDict.ContainsKey(reagent.Prototype))
                            reagentDict[reagent.Prototype] += quantity;
                        else
                            reagentDict.Add(reagent.Prototype, quantity);
                    }
                }
            }
        }

        // Check recipes
        var portionedRecipe = _cookingInstrument.GetSatisfiedPortionedRecipe(
            cookingInstrument, solidsDict, reagentDict, component.CookingTimer);

        _audio.PlayPvs(component.BeginCookingSound, uid);
        component.CookTimeRemaining = component.CookingTimer * component.CookTimeMultiplier;
        component.CurrentlyCookingRecipe = portionedRecipe;
        UpdateUserInterfaceState(uid, component);

        SetAppearance(uid, CookingMachineVisualState.Cooking, component);

        var audioStream = _audio.PlayPvs(component.LoopingSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        component.PlayingStream = audioStream?.Entity;

        component.Active = true;
    }

    public void StopCooking(EntityUid uid, CookingMachineComponent component)
    {
        component.Active = false;
        component.CookTimeRemaining = 0;
        component.CurrentlyCookingRecipe = (null, 0);
        UpdateUserInterfaceState(uid, component);
        SetAppearance(uid, CookingMachineVisualState.Idle, component);
        _audio.Stop(component.PlayingStream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CookingMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Active)
                continue;

            //check if there's still cook time left
            component.CookTimeRemaining -= frameTime;
            if (component.CookTimeRemaining > 0)
                continue;

            //this means the cooking machine has finished cooking.
            var ev = new BeforeCookingMachineFinished(component);
            RaiseLocalEvent(uid, ev);

            AddTemperature(component, component.CookingTimer);

            if (component.CurrentlyCookingRecipe.Item1 != null)
            {
                var coords = Transform(uid).Coordinates;
                for (var i = 0; i < component.CurrentlyCookingRecipe.Item2; i++)
                {
                    _cookingInstrument.SubtractContents(component.Storage, component.CurrentlyCookingRecipe.Item1);
                    Spawn(component.CurrentlyCookingRecipe.Item1.Result, coords);
                }
            }

            if (component.UseEntityStorage)
                _entityStorage.OpenStorage(uid);
            else
                _sharedContainer.EmptyContainer(component.Storage);

            StopCooking(uid, component);
            _audio.PlayPvs(component.FoodDoneSound, uid, AudioParams.Default.WithVolume(-1));
        }
    }

    /// <summary>
    ///     Adds temperature to every item in the microwave,
    ///     based on the time it took to microwave.
    /// </summary>
    /// <param name="machine">The machine that contains objects to heat up.</param>
    /// <param name="time">The time on the microwave, in seconds.</param>
    private void AddTemperature(CookingMachineComponent machine, float time)
    {
        if (machine.HeatPerSecond == 0)
            return;

        var heatToAdd = time * machine.HeatPerSecond;
        foreach (var entity in machine.Storage.ContainedEntities)
        {
            if (TryComp<TemperatureComponent>(entity, out var tempComp))
                _temperature.ChangeHeat(entity, heatToAdd, false, tempComp);

            if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                continue;

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
            {
                var solution = soln.Comp.Solution;
                if (solution.Temperature > machine.TemperatureUpperThreshold)
                    continue;

                _solutionContainer.AddThermalEnergy(soln, heatToAdd);
            }
        }
    }
}
