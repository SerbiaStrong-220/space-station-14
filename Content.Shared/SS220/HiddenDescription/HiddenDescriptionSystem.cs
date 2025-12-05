// Original code by Corvax dev team, no specific for SS220 license

using Content.Shared.Examine;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;

namespace Content.Shared.SS220.HiddenDescription;

public abstract class SharedHiddenDescriptionSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> entity, ref ExaminedEvent args)
    {
        PushExamineInformation(entity.Comp, ref args);

        Dirty(entity);
    }

    public void PushExamineInformation(HiddenDescriptionComponent component, ref ExaminedEvent args)
    {
        if (!TryComp<ExperienceComponent>(args.Examiner, out var experience))
            return;

        foreach (var (knowledge, locId) in component.Entries)
        {
            if (!_experience.HaveKnowledge((args.Examiner, experience), knowledge))
                continue;

            args.PushMarkup(Loc.GetString(locId), component.PushPriority);
        }
    }

}
