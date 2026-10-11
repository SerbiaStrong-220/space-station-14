// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

public sealed partial class ExperienceAdditionSystem : EntitySystem
{
    private readonly HashSet<EntityUid> _recalculateQueue = new(20);

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperienceComponent, AddExperience>(OnAddExperience);
        SubscribeLocalEvent<ExperienceComponent, RemoveExperience>(OnRemoveExperience);
        SubscribeLocalEvent<ExperienceComponent, TryChangeExperienceDefinition>(OnTryChangeExperienceDefinition);

        InitializeOtherSubscription();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _recalculateQueue)
        {
            var recalculateEv = new RecalculateEntityExperience();
            RaiseLocalEvent(ent, ref recalculateEv);
        }

        _recalculateQueue.Clear();
    }

    public void UpsertSpecifier(EntityUid uid, ExperienceAddSource source, ExperienceAddSpecifier? specifier)
    {
        var additionComp = EnsureComp<ExperienceAdditionComponent>(uid);

        if (specifier is { } resolvedSpecifier)
        {
            // Yes we drop old one, can be discussed
            additionComp.Sources[source] = resolvedSpecifier;
        }
        else
        {
            additionComp.Sources.Remove(source);
        }

        DirtyField(uid, additionComp, nameof(ExperienceAdditionComponent.Sources));

        EnqueueRecalculation(uid);
    }

    public void UpsertAdminForcedSpecifier(EntityUid uid, ExperienceAddSource source, ExperienceAddSpecifier? specifier)
    {
        var additionComp = EnsureComp<ExperienceAdditionComponent>(uid);

        additionComp.Forced = true;
        DirtyField(uid, additionComp, nameof(ExperienceAdditionComponent.Forced));

        UpsertSpecifier(uid, source, specifier);
    }

    public void RemoveAdminAdded(EntityUid uid)
    {
        if (!TryComp<ExperienceAdditionComponent>(uid, out var comp))
            return;

        comp.Sources.Remove(ExperienceAddSource.Admin);
        comp.Forced = false;

        DirtyField(uid, comp, nameof(ExperienceAdditionComponent.Sources));
        DirtyField(uid, comp, nameof(ExperienceAdditionComponent.Forced));

        EnqueueRecalculation(uid);
    }

    public void EnqueueRecalculation(EntityUid uid)
    {
        _recalculateQueue.Add(uid);
    }

    private void OnAddExperience(Entity<ExperienceComponent> entity, ref AddExperience args)
    {
        UpsertSpecifier(entity, args.Source, args.Specifier);
    }

    private void OnRemoveExperience(Entity<ExperienceComponent> entity, ref RemoveExperience args)
    {
        UpsertSpecifier(entity, args.Source, null);
    }

    private void OnTryChangeExperienceDefinition(Entity<ExperienceComponent> entity, ref TryChangeExperienceDefinition args)
    {
        // NOTE: for now null is just dev qol
        // NOTE2: it is also used to skip changing if force=true
        if (args.DefinitionId is null)
            return;

        var additionComp = EnsureComp<ExperienceAdditionComponent>(entity);

        if (args.Forced)
            additionComp.DefinitionId = args.DefinitionId;
        else
            additionComp.DefinitionId ??= args.DefinitionId;

        EnqueueRecalculation(entity);
        args.Changed = additionComp.DefinitionId == args.DefinitionId;

        if (args.Changed)
            DirtyField(entity, additionComp, nameof(ExperienceAdditionComponent.DefinitionId));
    }
}

[ByRefEvent]
public readonly record struct AddExperience(ExperienceAddSource Source, ExperienceAddSpecifier Specifier);

[ByRefEvent]
public readonly record struct RemoveExperience(ExperienceAddSource Source);

[ByRefEvent]
public record struct TryChangeExperienceDefinition(ProtoId<ExperienceDefinitionPrototype>? DefinitionId, bool Forced, bool Changed = false);
