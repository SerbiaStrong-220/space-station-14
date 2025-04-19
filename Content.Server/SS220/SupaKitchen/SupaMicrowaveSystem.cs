// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Construction.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Lightning;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Systems;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.SS220.SupaKitchen;
public sealed class SupaMicrowaveSystem : CookingInstrumentSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string MalfunctionSpark = "Spark";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupaMicrowaveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupaMicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: [typeof(AnchorableSystem)]);
        SubscribeLocalEvent<SupaMicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<SupaMicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SupaMicrowaveComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<SupaMicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<SupaMicrowaveComponent, IdCardMicrowavedEvent>(OnIdCardMicrowaved);

        // UI event listeners
        SubscribeLocalEvent<SupaMicrowaveComponent, MicrowaveStartCookMessage>((u, c, m) => StartCooking(u, c, m.Actor));
        SubscribeLocalEvent<SupaMicrowaveComponent, MicrowaveEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<SupaMicrowaveComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
        SubscribeLocalEvent<SupaMicrowaveComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);

        SubscribeLocalEvent<SupaMicrowaveComponent, SuicideEvent>(OnSuicide);
    }

    private void OnInit(Entity<SupaMicrowaveComponent> entity, ref ComponentInit ags)
    {
        entity.Comp.Storage = _container.EnsureContainer<Container>(entity, entity.Comp.StorageName);
        CheckPowered(entity);
    }

    private void OnPowerChanged(Entity<SupaMicrowaveComponent> entity, ref PowerChangedEvent args)
    {
        if (entity.Comp.CurrentState is SupaMicrowaveState.Broken)
            return;

        if (!args.Powered)
        {
            StopCooking(entity);
            SetMachineState(entity, SupaMicrowaveState.UnPowered);
        }
        else if (entity.Comp.CurrentState is SupaMicrowaveState.UnPowered)
        {
            SetMachineState(entity, SupaMicrowaveState.Idle);
        }
    }

    private void OnInteractUsing(Entity<SupaMicrowaveComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!(TryComp<ApcPowerReceiverComponent>(entity, out var apc) && apc.Powered))
        {
            _popupSystem.PopupEntity(
                Loc.GetString("supa-microwave-component-interact-using-no-power", ("machine", MetaData(entity).EntityName)),
                entity,
                args.User);
            return;
        }

        if (entity.Comp.CurrentState is SupaMicrowaveState.Broken)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("supa-microwave-component-interact-using-broken", ("machine", MetaData(entity).EntityName)),
                entity,
                args.User);
            return;
        }

        if (!HasComp<ItemComponent>(args.Used))
        {
            _popupSystem.PopupEntity(Loc.GetString("supa-microwave-component-interact-using-transfer-fail"),
                entity,
                args.User);
            return;
        }

        if (entity.Comp.Storage.Count >= entity.Comp.Capacity)
        {
            _popupSystem.PopupEntity(Loc.GetString("supa-microwave-component-interact-full"), entity, args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, entity.Comp.Storage);
        UpdateUserInterfaceState(entity);
    }

    private void OnSolutionChange(Entity<SupaMicrowaveComponent> entity, ref SolutionChangedEvent args)
    {
        UpdateUserInterfaceState(entity);
    }

    private void OnBreak(Entity<SupaMicrowaveComponent> entity, ref BreakageEventArgs args)
    {
        StopCooking(entity);

        _sharedContainer.EmptyContainer(entity.Comp.Storage);

        SetMachineState(entity, SupaMicrowaveState.Broken);
    }

    private void OnSignalReceived(Entity<SupaMicrowaveComponent> entity, ref SignalReceivedEvent args)
    {
        if (args.Port == entity.Comp.OnPort.Id)
            StartCooking(entity, entity, args.Trigger);
    }

    private void OnIdCardMicrowaved(Entity<SupaMicrowaveComponent> entity, ref IdCardMicrowavedEvent args)
    {
        if (args.Cancelled ||
            entity.Comp.CanMicrowaveIdsSafely ||
            !_random.Prob(entity.Comp.IdCardExplosionChance))
            return;

        Explode(entity);
        args.Cancel();
    }

    #region ui_messages
    private void OnEjectMessage(Entity<SupaMicrowaveComponent> entity, ref MicrowaveEjectMessage args)
    {
        if (!HasContents(entity.Comp) || entity.Comp.CurrentState != SupaMicrowaveState.Idle)
            return;

        _sharedContainer.EmptyContainer(entity.Comp.Storage);

        _audio.PlayPvs(entity.Comp.ClickSound, entity, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(entity);
    }

    private void OnEjectIndex(Entity<SupaMicrowaveComponent> entity, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (!HasContents(entity.Comp) || entity.Comp.CurrentState != SupaMicrowaveState.Idle)
            return;

        _container.Remove(GetEntity(args.EntityID), entity.Comp.Storage);
        UpdateUserInterfaceState(entity);
    }

    private void OnSelectTime(Entity<SupaMicrowaveComponent> entity, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!HasContents(entity.Comp) || entity.Comp.CurrentState != SupaMicrowaveState.Idle)
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > entity.Comp.MaxCookingTimer)
            return;

        entity.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        entity.Comp.CookingTimer = args.NewCookTime;
        _audio.PlayPvs(entity.Comp.ClickSound, entity, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(entity);
    }
    #endregion

    private void OnSuicide(Entity<SupaMicrowaveComponent> entity, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        //args.SetHandled(SuicideKind.Heat);
        args.Handled = true;
        var victim = args.Victim;
        var headCount = 0;

        if (TryComp<BodyComponent>(victim, out var body))
        {
            var headSlots = _bodySystem.GetBodyChildrenOfType(victim, BodyPartType.Head, body);

            foreach (var part in headSlots)
            {
                _sharedContainer.Insert(part.Id, entity.Comp.Storage);
                headCount++;
            }
        }

        var othersMessage = headCount > 1
            ? Loc.GetString("supa-microwave-component-suicide-multi-head-others-message", ("victim", victim))
            : Loc.GetString("supa-microwave-component-suicide-others-message", ("victim", victim));

        var selfMessage = headCount > 1
            ? Loc.GetString("supa-microwave-component-suicide-multi-head-message")
            : Loc.GetString("supa-microwave-component-suicide-message");

        _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        _popupSystem.PopupEntity(selfMessage, victim, victim);

        _audio.PlayPvs(entity.Comp.ClickSound, entity, AudioParams.Default.WithVolume(-2));
        entity.Comp.CookingTimer = 10;

        StartCooking(entity, args.Victim);
    }

    public static bool HasContents(SupaMicrowaveComponent component)
    {
        return component.Storage.ContainedEntities.Any();
    }

    public void SetMachineState(Entity<SupaMicrowaveComponent> entity, SupaMicrowaveState state)
    {
        entity.Comp.CurrentState = state;
        UpdateAppearance(entity);
        UpdateUserInterfaceState(entity);
    }

    public void UpdateAppearance(Entity<SupaMicrowaveComponent> entity)
    {
        SupaMicrowaveVisualState display;
        switch (entity.Comp.CurrentState)
        {
            case SupaMicrowaveState.UnPowered:
            case SupaMicrowaveState.Idle:
                display = SupaMicrowaveVisualState.Idle;
                break;
            case SupaMicrowaveState.Cooking:
                display = SupaMicrowaveVisualState.Cooking;
                break;
            case SupaMicrowaveState.Broken:
                display = SupaMicrowaveVisualState.Broken;
                break;
            default:
                display = SupaMicrowaveVisualState.Idle;
                break;
        }

        _appearance.SetData(entity, PowerDeviceVisuals.VisualState, display);
    }

    public void UpdateUserInterfaceState(Entity<SupaMicrowaveComponent> entity)
    {
        var isBusy = entity.Comp.CurrentState != SupaMicrowaveState.Idle;
        var timeEnd = entity.Comp.CookTimeRemaining > 0
            ? _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.CookTimeRemaining)
            : TimeSpan.Zero;

        _userInterface.SetUiState(entity.Owner, SupaMicrowaveUiKey.Key, new MicrowaveUpdateUserInterfaceState(
            GetNetEntityArray(entity.Comp.Storage.ContainedEntities.ToArray()),
            isBusy,
            entity.Comp.CurrentCookTimeButtonIndex,
            entity.Comp.CookingTimer,
            timeEnd
        ));
    }

    public void CheckPowered(Entity<SupaMicrowaveComponent> entity)
    {
        if (TryComp<ApcPowerReceiverComponent>(entity, out var apcReceiver) && !apcReceiver.Powered)
            SetMachineState(entity, SupaMicrowaveState.UnPowered);
        else
            SetMachineState(entity, SupaMicrowaveState.Idle);
    }

    public void Break(Entity<SupaMicrowaveComponent> entity)
    {
        SetMachineState(entity, SupaMicrowaveState.Broken);
        _audio.PlayPvs(entity.Comp.ItemBreakSound, entity);

        UpdateUserInterfaceState(entity);
    }

    public void StartCooking(Entity<SupaMicrowaveComponent> entity, EntityUid? whoStarted = null)
    {
        StartCooking(entity, entity, whoStarted);
    }

    public void StartCooking(EntityUid uid, SupaMicrowaveComponent component, EntityUid? whoStarted = null)
    {
        if (!HasContents(component) || component.CurrentState != SupaMicrowaveState.Idle)
            return;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();

        foreach (var item in component.Storage.ContainedEntities.ToList())
        {
            var ev = new BeingMicrowavedEvent(uid, whoStarted);
            RaiseLocalEvent(item, ev);

            if (_tag.HasTag(item, "Metal"))
            {
                EnsureMalfusion((uid, component));
            }

            // destroy microwave
            if (_tag.HasTag(item, "MicrowaveMachineUnsafe"))
            {
                Break((uid, component));
                return;
            }

            if (_tag.HasTag(item, "MicrowaveSelfUnsafe") || _tag.HasTag(item, "Plastic"))
            {
                var junk = Spawn(component.FailureResult, Transform(uid).Coordinates);
                _sharedContainer.Insert(junk, component.Storage);
                QueueDel(item);
            }

            if (ev.Handled)
            {
                UpdateUserInterfaceState((uid, component));
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
        var portionedRecipe = GetSatisfiedPortionedRecipe(
            component, solidsDict, reagentDict, (uint)(component.CookingTimer * component.CookTimeMultiplier));

        _audio.PlayPvs(component.BeginCookingSound, uid);
        component.CookTimeRemaining = component.CookingTimer;
        component.CurrentlyCookingRecipe = portionedRecipe;

        SetMachineState((uid, component), SupaMicrowaveState.Cooking);
        UpdateAppearance((uid, component));
        UpdateUserInterfaceState((uid, component));

        var audioStream = _audio.PlayPvs(component.LoopingSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        component.PlayingStream = audioStream?.Entity;
    }

    public void StopCooking(Entity<SupaMicrowaveComponent> entity)
    {
        entity.Comp.CookTimeRemaining = 0;
        entity.Comp.CurrentlyCookingRecipe = (null, 0);

        if (entity.Comp.CurrentState is SupaMicrowaveState.Cooking)
        {
            SetMachineState(entity, SupaMicrowaveState.Idle);
            UpdateAppearance(entity);
        }

        entity.Comp.MalfunctionTime = TimeSpan.Zero;
        UpdateUserInterfaceState(entity);
        _audio.Stop(entity.Comp.PlayingStream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SupaMicrowaveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CurrentState != SupaMicrowaveState.Cooking)
                continue;

            //check if there's still cook time left
            component.CookTimeRemaining -= frameTime;
            if (component.CookTimeRemaining > 0)
                continue;

            RollMalfunction((uid, component));
            AddTemperature(component, component.CookingTimer);

            if (component.CurrentlyCookingRecipe.Item1 != null)
            {
                var coords = Transform(uid).Coordinates;
                for (var i = 0; i < component.CurrentlyCookingRecipe.Item2; i++)
                {
                    SubtractContents(component.Storage, component.CurrentlyCookingRecipe.Item1);
                    Spawn(component.CurrentlyCookingRecipe.Item1.Result, coords);
                }
            }

            _sharedContainer.EmptyContainer(component.Storage);

            StopCooking((uid, component));
            _audio.PlayPvs(component.FoodDoneSound, uid, AudioParams.Default.WithVolume(-1));
        }
    }

    /// <summary>
    ///     Adds temperature to every item in the microwave,
    ///     based on the time it took to microwave.
    /// </summary>
    /// <param name="machine">The machine that contains objects to heat up.</param>
    /// <param name="time">The time on the microwave, in seconds.</param>
    private void AddTemperature(SupaMicrowaveComponent machine, float time)
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
                if (solution.Temperature > machine.HeatingThreshold)
                    continue;

                _solutionContainer.AddThermalEnergy(soln, heatToAdd);
            }
        }
    }

    /// <summary>
    /// Handles the attempted cooking of unsafe objects
    /// </summary>
    private void RollMalfunction(Entity<SupaMicrowaveComponent> entity)
    {
        if (entity.Comp.MalfunctionTime == TimeSpan.Zero || entity.Comp.MalfunctionTime > _gameTiming.CurTime)
            return;

        var interval = _random.Next(entity.Comp.MalfunctionInterval.Min, entity.Comp.MalfunctionInterval.Max);
        entity.Comp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(interval);
        if (_random.Prob(entity.Comp.ExplosionChance))
        {
            Explode(entity);
            return;  // microwave is fucked, stop the cooking.
        }

        if (_random.Prob(entity.Comp.LightningChance))
            _lightning.ShootRandomLightnings(entity, 1.0f, 2, MalfunctionSpark, triggerLightningEvents: false);
    }

    /// <summary>
    /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
    /// </summary>
    public void Explode(Entity<SupaMicrowaveComponent> entity)
    {
        SetMachineState(entity, SupaMicrowaveState.Broken);
        _explosion.TriggerExplosive(entity);
        if (TryComp<MachineComponent>(entity, out var machine))
        {
            _container.CleanContainer(machine.BoardContainer);
            _container.EmptyContainer(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(entity)} exploded from unsafe cooking!");
    }

    public void EnsureMalfusion(Entity<SupaMicrowaveComponent> entity)
    {
        var interval = _random.Next(entity.Comp.MalfunctionInterval.Min, entity.Comp.MalfunctionInterval.Max);
        entity.Comp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(interval);
    }
}
