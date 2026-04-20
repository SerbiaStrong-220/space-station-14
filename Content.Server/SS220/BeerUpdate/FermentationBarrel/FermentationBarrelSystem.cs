using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.SS220.BeerUpdate.FermentationBarrel;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.BeerUpdate.FermentationBarrel;

public sealed class FermentationBarrelSystem : EntitySystem
{
    [Dependency] private readonly ChemicalReactionSystem _reactions = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    private List<ReactionPrototype> _fermentationReactions = new();

    public override void Initialize()
    {
        base.Initialize();

        _fermentationReactions = _prototypes
            .EnumeratePrototypes<ReactionPrototype>()
            .Where(r => r.Fermentation)
            .OrderBy(r => r.FermentationDuration)
            .ToList();

        SubscribeLocalEvent<FermentationBarrelComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FermentationBarrelComponent, BoundUIOpenedEvent>(OnBoundUIOpenedEvent);
        SubscribeLocalEvent<FermentationBarrelComponent, FermentationBarrelToggleEvent>(OnToggleEvent);
        SubscribeLocalEvent<FermentationBarrelComponent, FermentationBarrelModeChangeEvent>(OnModeChangeEvent);
    }

    private void OnStartup(Entity<FermentationBarrelComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.IsDrawMode = false;
        SetSolutionMode(ent, locked: false, drawMode: false);
    }

    private void OnBoundUIOpenedEvent(Entity<FermentationBarrelComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnToggleEvent(Entity<FermentationBarrelComponent> ent, ref FermentationBarrelToggleEvent args)
    {
        if (ent.Comp.IsActive)
        {
            Stop(ent);
        }
        else
        {
            Start(ent);
        }
    }

    private void OnModeChangeEvent(Entity<FermentationBarrelComponent> ent, ref FermentationBarrelModeChangeEvent args)
    {
        if (ent.Comp.IsActive)
            return;

        ent.Comp.IsDrawMode = !ent.Comp.IsDrawMode;
        SetSolutionMode(ent, locked: false, drawMode: ent.Comp.IsDrawMode);
        UpdateUiState(ent);
    }

    private void Start(Entity<FermentationBarrelComponent> ent)
    {
        if (ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = true;
        ent.Comp.ElapsedTime = 0f;
        ent.Comp.ReactionsFired.Clear();

        SetSolutionMode(ent, locked: true);

        EnsureComp<ActiveFermentationBarrelComponent>(ent);
        Dirty(ent);
        UpdateUiState(ent);
    }

    private void Stop(Entity<FermentationBarrelComponent> ent)
    {
        if (!ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = false;
        ent.Comp.ElapsedTime = 0f;

        SetSolutionMode(ent, locked: false, drawMode: ent.Comp.IsDrawMode);

        RemComp<ActiveFermentationBarrelComponent>(ent);
        Dirty(ent);
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<FermentationBarrelComponent> ent)
    {
        UpdateUiState(ent, ent.Comp);
    }

    private float _uiUpdateAccumulator = 0f;
    private const float UiUpdateInterval = 0.1f; // Update UI every 100ms

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _uiUpdateAccumulator += frameTime;
        var shouldUpdateUi = _uiUpdateAccumulator >= UiUpdateInterval;
        if (shouldUpdateUi)
            _uiUpdateAccumulator = 0f;

        var query = EntityQueryEnumerator<ActiveFermentationBarrelComponent, FermentationBarrelComponent>();
        while (query.MoveNext(out var uid, out _, out var barrel))
        {
            barrel.ElapsedTime += frameTime;

            if (!_solutions.TryGetSolution(uid, barrel.SolutionName, out var soln))
                continue;

            foreach (var reaction in _fermentationReactions)
            {
                if (barrel.ReactionsFired.TryGetValue(reaction.ID, out var fired) && fired)
                    continue;

                if (barrel.ElapsedTime < reaction.FermentationDuration)
                    break;

                if (!_reactions.TryCanReact(soln.Value, reaction, null, out var unitReactions))
                    continue;

                var attempt = new FermentationBarrelReactionAttemptEvent(reaction);
                RaiseLocalEvent(uid, attempt);
                if (attempt.Cancelled)
                    continue;

                _reactions.TryPerformReaction(soln.Value, reaction, unitReactions);
                barrel.ReactionsFired[reaction.ID] = true;
            }

            if (shouldUpdateUi)
                UpdateUiState(uid, barrel);

            Dirty(uid, barrel);
        }
    }

    private void UpdateUiState(EntityUid uid, FermentationBarrelComponent barrel)
    {
        ReagentQuantity[]? reagents = null;
        if (_solutions.TryGetSolution(uid, barrel.SolutionName, out var solution))
        {
            reagents = solution.Value.Comp.Solution.Contents
                .Select(r => new ReagentQuantity(r.Reagent, r.Quantity))
                .ToArray();
        }

        var state = new FermentationBarrelInterfaceState(barrel.IsActive, barrel.ElapsedTime, reagents);
        _userInterfaceSystem.SetUiState(uid, FermentationBarrelUiKey.Key, state);
    }

    private void SetSolutionMode(Entity<FermentationBarrelComponent> ent, bool locked, bool drawMode = false)
    {
        var (uid, comp) = ent;

        RemComp<RefillableSolutionComponent>(uid);
        RemComp<DrainableSolutionComponent>(uid);

        if (!locked)
        {
            if (drawMode)
            {
                var drainable = EnsureComp<DrainableSolutionComponent>(uid);
                drainable.Solution = comp.SolutionName;
            }
            else
            {
                var refillable = EnsureComp<RefillableSolutionComponent>(uid);
                refillable.Solution = comp.SolutionName;
            }
        }
    }
}
