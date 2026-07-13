// SS220 Changeling
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Changeling.Objectives;

/// <summary>
/// Assigns the fixed changeling objective package and evaluates changeling-specific objective conditions.
/// </summary>
public sealed class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _identity = default!;
    [Dependency] private readonly SharedIdCardSystem _idCards = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjective = default!;
    [Dependency] private readonly TargetSystem _targets = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(OnChangelingSelected);

        SubscribeLocalEvent<ChangelingAbsorbDnaConditionComponent, ObjectiveAssignedEvent>(OnAbsorbDnaAssigned);
        SubscribeLocalEvent<ChangelingAbsorbDnaConditionComponent, ObjectiveAfterAssignEvent>(OnAbsorbDnaAfterAssigned);
        SubscribeLocalEvent<ChangelingAbsorbDnaConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbDnaProgress);

        SubscribeLocalEvent<ChangelingStealBrainConditionComponent, ObjectiveAssignedEvent>(OnStealBrainAssigned);
        SubscribeLocalEvent<ChangelingStealBrainConditionComponent, ObjectiveAfterAssignEvent>(OnStealBrainAfterAssigned);
        SubscribeLocalEvent<ChangelingStealBrainConditionComponent, ObjectiveGetProgressEvent>(OnStealBrainProgress);

        SubscribeLocalEvent<ChangelingKillAndImpersonateConditionComponent, ObjectiveAssignedEvent>(OnKillAndImpersonateAssigned);
        SubscribeLocalEvent<ChangelingKillAndImpersonateConditionComponent, ObjectiveGetProgressEvent>(OnKillAndImpersonateProgress);

        SubscribeLocalEvent<ChangelingKillStationAiConditionComponent, ObjectiveAssignedEvent>(OnKillAiAssigned);
        SubscribeLocalEvent<ChangelingKillStationAiConditionComponent, ObjectiveGetProgressEvent>(OnKillAiProgress);

        SubscribeLocalEvent<ChangelingEscapeAliveConditionComponent, ObjectiveGetProgressEvent>(OnEscapeAliveProgress);
    }

    private void OnChangelingSelected(Entity<ChangelingRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.EntityUid, out var mindId, out var mind))
        {
            Log.Error($"Selected changeling {ToPrettyString(args.EntityUid):player} has no mind; objectives were not assigned.");
            return;
        }

        AddRequiredObjective((mindId, mind), ent.Comp.AbsorbDnaObjective);
        AddTheftObjective((mindId, mind), ent.Comp);

        var hasStationAi = GetAliveStationAis((mindId, mind)).Count != 0;
        var primaryKillObjective = hasStationAi
            ? ent.Comp.KillAiObjective
            : ent.Comp.KillAndImpersonateObjective;
        var fallbackKillObjective = hasStationAi
            ? ent.Comp.KillAndImpersonateObjective
            : ent.Comp.KillAiObjective;
        if (!AddRequiredObjective((mindId, mind), primaryKillObjective))
            AddRequiredObjective((mindId, mind), fallbackKillObjective);

        AddRequiredObjective((mindId, mind), ent.Comp.EscapeObjective);
    }

    private bool AddRequiredObjective(Entity<MindComponent> mind, EntProtoId<ObjectiveComponent> prototype)
    {
        if (_mind.TryAddObjective(mind.Owner, mind.Comp, prototype.Id))
            return true;

        Log.Error($"Failed to assign required changeling objective {prototype} to {_mind.MindOwnerLoggingString(mind.Comp)}.");
        return false;
    }

    private void AddTheftObjective(Entity<MindComponent> mind, ChangelingRuleComponent rule)
    {
        var brainFirst = _random.Prob(rule.StealBrainChance);
        if (brainFirst && _mind.TryAddObjective(mind.Owner, mind.Comp, rule.StealBrainObjective.Id))
            return;

        var itemObjectives = rule.ValuableItemObjectives.ToList();
        _random.Shuffle(itemObjectives);
        foreach (var objective in itemObjectives)
        {
            if (_mind.TryAddObjective(mind.Owner, mind.Comp, objective.Id))
                return;
        }

        if (!brainFirst && _mind.TryAddObjective(mind.Owner, mind.Comp, rule.StealBrainObjective.Id))
            return;

        Log.Error($"Failed to assign a theft objective to changeling {_mind.MindOwnerLoggingString(mind.Comp)}.");
    }

    private void OnAbsorbDnaAssigned(Entity<ChangelingAbsorbDnaConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (ent.Comp.MinGenomes < 1 || ent.Comp.MaxGenomes < ent.Comp.MinGenomes)
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.TargetGenomes = _random.Next(ent.Comp.MinGenomes, ent.Comp.MaxGenomes + 1);
    }

    private void OnAbsorbDnaAfterAssigned(Entity<ChangelingAbsorbDnaConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(ent, Loc.GetString("changeling-objective-absorb-dna-name", ("count", ent.Comp.TargetGenomes)), args.Meta);
        _metaData.SetEntityDescription(ent,
            Loc.GetString("changeling-objective-absorb-dna-desc"),
            args.Meta);
    }

    private void OnAbsorbDnaProgress(Entity<ChangelingAbsorbDnaConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } body ||
            !TryGetPersistentChangelingIdentity(body, out var identity) ||
            ent.Comp.TargetGenomes <= 0)
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = Math.Clamp(identity.AbsorbedGenomes.Count / (float) ent.Comp.TargetGenomes, 0f, 1f);
    }

    private bool TryGetPersistentChangelingIdentity(EntityUid body, [NotNullWhen(true)] out ChangelingIdentityComponent? identity)
    {
        if (TryComp(body, out identity))
            return true;

        // Lesser Form keeps all persistent changeling state on the paused polymorph parent while the mind controls
        // the vulnerable child. Cumulative DNA progress must follow that state rather than the temporary body.
        if (TryComp<PolymorphedEntityComponent>(body, out var polymorphed) &&
            polymorphed.Parent is { } parent &&
            TryComp(parent, out identity))
        {
            return true;
        }

        identity = null;
        return false;
    }

    private void OnStealBrainAssigned(Entity<ChangelingStealBrainConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var owner = new Entity<MindComponent>(args.MindId, args.Mind);
        var candidates = _targets.GetAliveHumans(args.MindId)
            .Where(candidate => IsEligibleCrew(owner, candidate))
            .ToList();
        _random.Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (candidate.Comp.OwnedEntity is not { } body ||
                !TryComp<BodyComponent>(body, out var bodyComp) ||
                bodyComp.Organs == null)
                continue;

            EntityUid? brain = null;
            foreach (var organ in bodyComp.Organs.ContainedEntities)
            {
                if (!HasComp<BrainComponent>(organ))
                    continue;

                brain = organ;
                break;
            }

            if (brain == null)
                continue;

            ent.Comp.TargetMind = candidate.Owner;
            ent.Comp.TargetBody = body;
            ent.Comp.TargetBrain = brain;
            return;
        }

        args.Cancelled = true;
    }

    private void OnStealBrainAfterAssigned(Entity<ChangelingStealBrainConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (ent.Comp.TargetMind is not { } targetMind || !TryComp<MindComponent>(targetMind, out var mind))
            return;

        var targetName = ent.Comp.TargetBody is { } body
            ? Identity.Name(body, EntityManager)
            : mind.CharacterName ?? Loc.GetString("changeling-objective-target-fallback");
        _metaData.SetEntityName(ent, Loc.GetString("changeling-objective-steal-brain-name", ("target", targetName)), args.Meta);
        _metaData.SetEntityDescription(ent,
            Loc.GetString("changeling-objective-steal-brain-desc"),
            args.Meta);
    }

    private void OnStealBrainProgress(Entity<ChangelingStealBrainConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.OwnedEntity is { } body &&
                        IsInPossession(body, ent.Comp.TargetBrain)
            ? 1f
            : 0f;
    }

    private void OnKillAndImpersonateAssigned(
        Entity<ChangelingKillAndImpersonateConditionComponent> ent,
        ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var targetObjective))
        {
            args.Cancelled = true;
            return;
        }

        var owner = new Entity<MindComponent>(args.MindId, args.Mind);
        var candidates = _targets.GetAliveHumans(args.MindId)
            .Where(candidate => IsEligibleCrew(owner, candidate))
            .ToList();
        _random.Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (candidate.Comp.OwnedEntity is not { } body || _identity.GetGenomeId(body) is not { } genome)
                continue;

            ent.Comp.TargetMind = candidate.Owner;
            ent.Comp.TargetBody = body;
            ent.Comp.TargetGenome = genome;
            if (_idCards.TryFindIdCard(body, out var idCard))
                ent.Comp.TargetIdCard = idCard.Owner;

            _targetObjective.SetTarget(ent, candidate.Owner, targetObjective);
            return;
        }

        args.Cancelled = true;
    }

    private void OnKillAndImpersonateProgress(
        Entity<ChangelingKillAndImpersonateConditionComponent> ent,
        ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.TargetMind is not { } targetMind ||
            !TryComp<MindComponent>(targetMind, out var targetMindComp) ||
            !_mind.IsCharacterDeadIc(targetMindComp))
        {
            args.Progress = 0f;
            return;
        }

        if (args.Mind.OwnedEntity is not { } body)
        {
            args.Progress = 0.5f;
            return;
        }

        var matchingGenome = TryComp<ChangelingIdentityComponent>(body, out var identity) &&
                             identity.CurrentGenome == ent.Comp.TargetGenome;
        var carryingId = IsInPossession(body, ent.Comp.TargetIdCard);
        var escapedInDisguise = _emergencyShuttle.IsTargetEscaping(body) && (matchingGenome || carryingId);
        args.Progress = escapedInDisguise ? 1f : 0.5f;
    }

    private void OnKillAiAssigned(Entity<ChangelingKillStationAiConditionComponent> ent,
        ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var targetObjective))
        {
            args.Cancelled = true;
            return;
        }

        var candidates = GetAliveStationAis((args.MindId, args.Mind));
        if (candidates.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var target = _random.Pick(candidates);
        ent.Comp.TargetMind = target.Owner;
        _targetObjective.SetTarget(ent, target.Owner, targetObjective);
    }

    private void OnKillAiProgress(Entity<ChangelingKillStationAiConditionComponent> ent,
        ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.TargetMind is { } target &&
                        TryComp<MindComponent>(target, out var targetMind) &&
                        _mind.IsCharacterDeadIc(targetMind)
            ? 1f
            : 0f;
    }

    private List<Entity<MindComponent>> GetAliveStationAis(Entity<MindComponent> owner)
    {
        var ais = new HashSet<Entity<MindComponent>>();
        _stationAi.AddAliveAis(ais, owner.Owner);
        return ais.Where(candidate => IsOnOwnersStation(owner, candidate)).ToList();
    }

    private bool IsEligibleCrew(Entity<MindComponent> owner, Entity<MindComponent> candidate)
    {
        return _roles.MindHasRole<JobRoleComponent>(candidate.Owner, out _) &&
               IsOnOwnersStation(owner, candidate);
    }

    private bool IsOnOwnersStation(Entity<MindComponent> owner, Entity<MindComponent> candidate)
    {
        if (candidate.Comp.OwnedEntity is not { } candidateBody)
            return false;

        if (owner.Comp.OwnedEntity is not { } ownerBody ||
            _station.GetOwningStation(ownerBody) is not { } ownerStation)
        {
            // During very early round-start assignment a body may not yet be parented to its station grid.
            return true;
        }

        return _station.GetOwningStation(candidateBody) == ownerStation;
    }

    private void OnEscapeAliveProgress(Entity<ChangelingEscapeAliveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.OwnedEntity is { } body &&
                        !_mind.IsCharacterDeadIc(args.Mind) &&
                        _emergencyShuttle.IsTargetEscaping(body)
            ? 1f
            : 0f;
    }

    private bool IsInPossession(EntityUid owner, EntityUid? target)
    {
        if (target == null || Deleted(target.Value))
            return false;

        if (owner == target)
            return true;

        if (TryComp<PullerComponent>(owner, out var puller) && puller.Pulling == target)
            return true;

        if (!TryComp<ContainerManagerComponent>(owner, out var manager))
            return false;

        var containers = new Stack<ContainerManagerComponent>();
        containers.Push(manager);
        while (containers.TryPop(out var current))
        {
            foreach (var container in current.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (contained == target)
                        return true;

                    if (TryComp<ContainerManagerComponent>(contained, out var nested))
                        containers.Push(nested);
                }
            }
        }

        return false;
    }
}
