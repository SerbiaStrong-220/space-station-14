using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.SS220.Pirates;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateObjectiveSystem : EntitySystem
{
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PirateLootValueConditionComponent, ObjectiveGetProgressEvent>(OnLootValueProgress);
        SubscribeLocalEvent<PirateLootValueConditionComponent, ObjectiveAfterAssignEvent>(OnLootValueAfterAssign);
        SubscribeLocalEvent<PirateCrewCaptureConditionComponent, ObjectiveAfterAssignEvent>(OnCrewCaptureAfterAssign);
        SubscribeLocalEvent<PirateCrewCaptureConditionComponent, ObjectiveGetProgressEvent>(OnCrewCaptureProgress);
    }

    private void OnLootValueProgress(Entity<PirateLootValueConditionComponent> objective,
        ref ObjectiveGetProgressEvent args)
    {
        var value = 0L;
        var rules = EntityQueryEnumerator<PirateGameRuleComponent>();
        if (rules.MoveNext(out _, out var rule))
            value = rule.TotalLootValue;

        _metaData.SetEntityDescription(objective.Owner,
            Loc.GetString("pirate-objective-loot-description", ("current", value), ("target", objective.Comp.Target)));
        args.Progress = objective.Comp.Target <= 0
            ? 1f
            : Math.Clamp((float) value / objective.Comp.Target, 0f, 1f);
    }

    private void OnLootValueAfterAssign(Entity<PirateLootValueConditionComponent> objective,
        ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(objective.Owner, Loc.GetString("pirate-objective-loot-title"), args.Meta);
        _metaData.SetEntityDescription(objective.Owner,
            Loc.GetString("pirate-objective-loot-description", ("current", 0), ("target", objective.Comp.Target)),
            args.Meta);
    }

    private void OnCrewCaptureAfterAssign(Entity<PirateCrewCaptureConditionComponent> objective,
        ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(objective.Owner, Loc.GetString("pirate-objective-captives-title"), args.Meta);
        UpdateCrewCaptureDescription(objective, 0, args.Meta);
    }

    private void OnCrewCaptureProgress(Entity<PirateCrewCaptureConditionComponent> objective,
        ref ObjectiveGetProgressEvent args)
    {
        EnsureCrewTargets(objective);
        var captured = 0;
        foreach (var target in objective.Comp.Targets)
        {
            if (!TryComp<MindComponent>(target, out var mind) ||
                mind.OwnedEntity is not { } body ||
                Deleted(body) ||
                !TryComp<MobStateComponent>(body, out var mobState) ||
                !_mobState.IsAlive(body, mobState) ||
                Transform(body).GridUid is not { } grid ||
                !HasComp<PirateBaseComponent>(grid))
            {
                continue;
            }

            captured++;
        }

        UpdateCrewCaptureDescription(objective, captured);
        args.Progress = objective.Comp.Target <= 0
            ? 1f
            : Math.Clamp((float) captured / objective.Comp.Target, 0f, 1f);
    }

    private void EnsureCrewTargets(Entity<PirateCrewCaptureConditionComponent> objective)
    {
        var rules = EntityQueryEnumerator<PirateGameRuleComponent>();
        if (!rules.MoveNext(out _, out var rule))
            return;

        for (var i = rule.CaptureTargets.Count - 1; i >= 0; i--)
        {
            var target = rule.CaptureTargets[i];
            if (Deleted(target) || _roles.MindHasRole<PirateCrewRoleComponent>(target))
                rule.CaptureTargets.RemoveAt(i);
        }

        var missingTargets = objective.Comp.Target - rule.CaptureTargets.Count;
        if (missingTargets > 0)
        {
            var candidates = new List<Entity<MindComponent>>();
            var humans = _target.GetAliveHumans();
            foreach (var human in humans)
            {
                if (_roles.MindHasRole<PirateCrewRoleComponent>(human.Owner) ||
                    rule.CaptureTargets.Contains(human.Owner))
                    continue;

                candidates.Add(human);
            }

            _random.Shuffle(candidates);
            var targetCount = Math.Min(missingTargets, candidates.Count);
            for (var i = 0; i < targetCount; i++)
                rule.CaptureTargets.Add(candidates[i].Owner);
        }

        for (var i = objective.Comp.Targets.Count - 1; i >= 0; i--)
        {
            if (!rule.CaptureTargets.Contains(objective.Comp.Targets[i]))
                objective.Comp.Targets.RemoveAt(i);
        }

        foreach (var target in rule.CaptureTargets)
        {
            if (!objective.Comp.Targets.Contains(target))
                objective.Comp.Targets.Add(target);
        }
    }

    private void UpdateCrewCaptureDescription(Entity<PirateCrewCaptureConditionComponent> objective, int captured,
        MetaDataComponent? metaData = null)
    {
        var names = new List<string>();
        foreach (var target in objective.Comp.Targets)
        {
            if (!TryComp<MindComponent>(target, out var mind))
                continue;

            names.Add(mind.CharacterName ?? Name(target));
        }

        var description = names.Count == 0
            ? Loc.GetString("pirate-objective-captives-awaiting-description")
            : Loc.GetString("pirate-objective-captives-description",
                ("current", captured),
                ("target", objective.Comp.Target),
                ("targets", string.Join(", ", names)));
        _metaData.SetEntityDescription(objective.Owner, description, metaData);
    }
}
