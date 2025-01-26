// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.DeviceLinking.Events;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.SupaKitchen.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Server.SS220.SupaKitchen.Systems;

public sealed partial class CookingConstantlySystem : SharedCookingConstantlySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly CookingInstrumentSystem _cookingInstrument = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly ApcSystem _apcSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CookingConstantlyComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<CookingConstantlyComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<CookingConstantlyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CookingConstantlyComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CookingConstantlyComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<CookingConstantlyComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<CookingConstantlyComponent, StorageAfterOpenEvent>(OnStorageOpen);

        SubscribeLocalEvent<CookingConstantlyComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CookingConstantlyComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<CookingConstantlyComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<CookingConstantlyComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CookingConstantlyComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CurrentState != CookingConstantlyState.Active ||
                component.CurrentCookingRecipe is null)
                continue;

            if (!HasContents(component))
                continue;

            component.PackCookingTime += frameTime;
            if (component.CurrentCookingRecipe.CookTime > component.PackCookingTime)
                continue;

            FinalizeCooking(uid, component);
        }
    }

    private void OnInit(EntityUid uid, CookingConstantlyComponent component, ComponentInit args)
    {
        if (component.UseEntityStorage)
            component.Container = _container.EnsureContainer<Container>(uid, EntityStorageSystem.ContainerName);
        else
            component.Container = _container.EnsureContainer<Container>(uid, "cooking_machine_entity_container");
    }

    private void OnMapInit(Entity<CookingConstantlyComponent> entity, ref MapInitEvent args)
    {
        if (!_apcSystem.IsPowered(entity, EntityManager))
            SetState(entity, entity, CookingConstantlyState.UnPowered);
    }

    private void OnPowerChanged(Entity<CookingConstantlyComponent> entity, ref PowerChangedEvent args)
    {
        var (uid, comp) = entity;
        if (comp.CurrentState is CookingConstantlyState.Broken)
            return;

        if (!args.Powered)
        {
            Deactivate(uid, comp, false);
            SetState(uid, comp, CookingConstantlyState.UnPowered);
        }
        else if (comp.CurrentState is CookingConstantlyState.UnPowered)
        {
            if (comp.LastState is CookingConstantlyState.Active)
                Activate(uid, comp);
            else
                SetState(uid, comp, comp.LastState);
        }
    }

    private void OnBreak(Entity<CookingConstantlyComponent> entity, ref BreakageEventArgs args)
    {
        Deactivate(entity, entity);

        if (entity.Comp.UseEntityStorage)
            _entityStorage.CloseStorage(entity);

        _container.EmptyContainer(entity.Comp.Container);

        SetState(entity, entity, CookingConstantlyState.Broken);
    }

    private void OnAlternativeVerb(Entity<CookingConstantlyComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        AlternativeVerb? newVerb = null;
        switch (entity.Comp.CurrentState)
        {
            case CookingConstantlyState.Idle:
                newVerb = new AlternativeVerb()
                {
                    Act = () => Activate(entity, entity, user),
                    Text = "Activate"
                };
                break;
            case CookingConstantlyState.Active:
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

    private void OnSignalReceived(Entity<CookingConstantlyComponent> entity, ref SignalReceivedEvent args)
    {
        if (args.Port == entity.Comp.OnPort.Id &&
            entity.Comp.CurrentState is CookingConstantlyState.Idle)
            Activate(entity, entity);

        if (args.Port == entity.Comp.TogglePort.Id)
        {
            switch (entity.Comp.CurrentState)
            {
                case CookingConstantlyState.Active:
                    Deactivate(entity, entity);
                    break;
                case CookingConstantlyState.Idle:
                    Activate(entity, entity);
                    break;
            }
        }
    }

    public void Activate(EntityUid uid, CookingConstantlyComponent component, EntityUid? user = null)
    {
        CycleCooking(uid, component);

        component.LastUser = user;
        SetState(uid, component, CookingConstantlyState.Active);

        _audio.PlayPvs(component.ActivateSound, uid);

        var audioStream = _audio.PlayPvs(component.LoopingSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        component.PlayingStream = GetNetEntity(audioStream?.Entity);
        Dirty(uid, component);
    }

    private void CycleCooking(EntityUid uid, CookingConstantlyComponent component)
    {
        if (!HasContents(component))
            return;

        if (IsPackChanged(uid, component))
        {
            component.PackCookingTime = 0;
            component.CurrentCookingRecipe = null;
        }

        if (component.CurrentCookingRecipe is null)
            component.CurrentCookingRecipe = GetCookingRecipe(uid, component);
    }

    private void FinalizeCooking(EntityUid uid, CookingConstantlyComponent component)
    {
        if (component.CurrentCookingRecipe is null)
            return;

        var container = component.Container;
        _cookingInstrument.SubtractContents(container, component.CurrentCookingRecipe);
        SpawnInContainerOrDrop(component.CurrentCookingRecipe.Result, uid, container.ID);
        CycleCooking(uid, component);
    }

    public bool IsPackChanged(EntityUid uid, CookingConstantlyComponent component, bool setNew = true)
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

    public bool HasContents(CookingConstantlyComponent component)
    {
        return component.Container.ContainedEntities.Any();
    }

    private CookingRecipePrototype? GetCookingRecipe(EntityUid uid, CookingConstantlyComponent component)
    {
        if (!TryComp<CookingInstrumentComponent>(uid, out var cookingInstrument))
            return null;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();

        foreach (var item in component.Container.ContainedEntities.ToList())
        {
            var metaData = MetaData(item); //this still begs for cooking refactor
            if (metaData.EntityPrototype == null)
                continue;

            if (!solidsDict.TryAdd(metaData.EntityPrototype.ID, 1))
                solidsDict[metaData.EntityPrototype.ID]++;

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

        return _cookingInstrument.GetSatisfiedPortionedRecipe(cookingInstrument, solidsDict, reagentDict, 0).Item1;
    }
}
