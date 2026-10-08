// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience;

namespace Content.Server.SS220.Experience;

public sealed partial class ExperienceAdditionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ExperienceComponent, AddExperience>(OnAddExperience);
        SubscribeLocalEvent<ExperienceComponent, RemoveExperience>(OnRemoveExperience);

        InitializeOtherSubscription();
    }

    private void OnAddExperience(Entity<ExperienceComponent> entity, ref AddExperience args)
    {
        UpsertSpecifier(entity, args.Source, args.Specifier);
    }

    private void OnRemoveExperience(Entity<ExperienceComponent> entity, ref RemoveExperience args)
    {
        UpsertSpecifier(entity, args.Source, null);
    }


    private void UpsertSpecifier(EntityUid uid, ExperienceAddSource source, ExperienceAddSpecifier? specifier)
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

        Dirty(uid, additionComp);

        CallRecalculation(uid);
    }

    private void CallRecalculation(EntityUid uid)
    {
        var recalculateEv = new RecalculateEntityExperience();
        RaiseLocalEvent(uid, ref recalculateEv);
    }
}

[ByRefEvent]
public record struct AddExperience(ExperienceAddSource Source, ExperienceAddSpecifier Specifier);

[ByRefEvent]
public record struct RemoveExperience(ExperienceAddSource Source);
