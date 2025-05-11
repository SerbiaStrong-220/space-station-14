// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.SS220.SupaKitchen.Systems;

public sealed partial class OvenSystem : SharedOvenSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly ApcSystem _apcSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvenComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<OvenComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<OvenComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<OvenComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<OvenComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OvenComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CurrentState != OvenState.Active ||
                component.CurrentCookingRecipe is null)
                continue;

            if (!HasContents(component))
                continue;

            AddTemperature(component, frameTime);
            component.PackCookingTime += frameTime;
            if (component.CurrentCookingRecipe.CookTime > component.PackCookingTime)
                continue;

            FinalizeCooking(uid, component);
        }
    }

    private void OnMapInit(Entity<OvenComponent> entity, ref MapInitEvent args)
    {
        if (!_apcSystem.IsPowered(entity, EntityManager))
            SetState(entity, entity, OvenState.UnPowered);
    }

    private void OnPowerChanged(Entity<OvenComponent> entity, ref PowerChangedEvent args)
    {
        var (uid, comp) = entity;
        if (comp.CurrentState is OvenState.Broken)
            return;

        if (!args.Powered)
        {
            Deactivate(uid, comp, false);
            SetState(uid, comp, OvenState.UnPowered);
        }
        else if (comp.CurrentState is OvenState.UnPowered)
        {
            if (comp.LastState is OvenState.Active)
                Activate(uid, comp);
            else
                SetState(uid, comp, comp.LastState);
        }
    }

    private void OnBreak(Entity<OvenComponent> entity, ref BreakageEventArgs args)
    {
        Deactivate(entity, entity);

        if (entity.Comp.UseEntityStorage)
            _entityStorage.CloseStorage(entity);

        _container.EmptyContainer(entity.Comp.Container);

        SetState(entity, entity, OvenState.Broken);
    }

    private void OnAlternativeVerb(Entity<OvenComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        AlternativeVerb? newVerb = null;
        switch (entity.Comp.CurrentState)
        {
            case OvenState.Idle:
                newVerb = new AlternativeVerb()
                {
                    Act = () => Activate(entity, entity, user),
                    Text = "Activate"
                };
                break;
            case OvenState.Active:
                newVerb = new AlternativeVerb()
                {
                    Act = () => Deactivate(entity, entity),
                    Text = "Deactivate"
                };
                break;
        }

        if (newVerb != null)
            args.Verbs.Add(newVerb);
    }

    private void OnSignalReceived(Entity<OvenComponent> entity, ref SignalReceivedEvent args)
    {
        if (args.Port == entity.Comp.OnPort.Id &&
            entity.Comp.CurrentState is OvenState.Idle)
            Activate(entity, entity);

        if (args.Port == entity.Comp.TogglePort.Id)
        {
            switch (entity.Comp.CurrentState)
            {
                case OvenState.Active:
                    Deactivate(entity, entity);
                    break;
                case OvenState.Idle:
                    Activate(entity, entity);
                    break;
            }
        }
    }

    public void Activate(EntityUid uid, OvenComponent component, EntityUid? user = null)
    {
        if (component.UseEntityStorage &&
            TryComp<EntityStorageComponent>(uid, out var entStorage) &&
            entStorage.Open)
            return;

        CycleCooking(uid, component);

        component.LastUser = user;
        SetState(uid, component, OvenState.Active);

        _audio.PlayPvs(component.ActivateSound, uid);

        var audioStream = _audio.PlayPvs(component.LoopingSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        component.PlayingStream = GetNetEntity(audioStream?.Entity);
        Dirty(uid, component);
    }

    private void CycleCooking(EntityUid uid, OvenComponent component)
    {
        if (!HasContents(component))
            return;

        if (IsPackChanged(uid, component))
        {
            component.PackCookingTime = 0;
            component.CurrentCookingRecipe = null;
        }

        component.CurrentCookingRecipe ??= GetCookingRecipe(uid, component);
    }

    private void FinalizeCooking(EntityUid uid, OvenComponent component)
    {
        if (component.CurrentCookingRecipe is null)
            return;

        var container = component.Container;
        SubtractContents(container, component.CurrentCookingRecipe);
        SpawnInContainerOrDrop(component.CurrentCookingRecipe.Result, uid, container.ID);
        _audio.PlayPvs(component.FoodDoneSound, uid);

        CycleCooking(uid, component);
    }

    public bool IsPackChanged(EntityUid uid, OvenComponent component, bool setNew = true)
    {
        var container = component.Container;
        var containedEntities = container.ContainedEntities.OrderBy(x => x).ToList();

        var isPackChanged = false;
        if (!component.EntityPack.SequenceEqual(containedEntities))
            isPackChanged = true;

        var reagentDict = new Dictionary<string, FixedPoint2>();
        foreach (var item in containedEntities)
        {
            if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                continue;

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
            {
                var solution = soln.Comp.Solution;
                {
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                            reagentDict[reagent.Prototype] += quantity;
                    }
                }
            }
        }

        reagentDict = reagentDict.OrderBy(x => x.Key).ToDictionary();
        if (!component.ReagentsPack.SequenceEqual(reagentDict))
            isPackChanged = true;

        if (setNew && isPackChanged)
        {
            component.EntityPack = containedEntities;
            component.ReagentsPack = reagentDict;
        }

        return isPackChanged;
    }

    public bool HasContents(OvenComponent component)
    {
        return component.Container.ContainedEntities.Any();
    }

    private CookingRecipePrototype? GetCookingRecipe(EntityUid uid, OvenComponent component)
    {
        var entities = component.Container.ContainedEntities;
        var portionedRecipe = GetSatisfiedPortionedRecipe(component, entities, 0);
        if (portionedRecipe.Item2 <= 0)
            return null;

        return portionedRecipe.Item1;
    }

    private void AddTemperature(OvenComponent component, float modifier = 1)
    {
        if (component.HeatPerSecond == 0)
            return;

        var heatToAdd = component.HeatPerSecond * modifier;
        foreach (var entity in component.Container.ContainedEntities)
        {
            if (!TryComp<TemperatureComponent>(entity, out var temperature))
                _temperature.ChangeHeat(entity, heatToAdd);

            if (TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
            {
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                {
                    var solution = soln.Comp.Solution;
                    if (solution.Temperature > component.HeatingThreshold)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, heatToAdd);
                }
            }
        }
    }
}
