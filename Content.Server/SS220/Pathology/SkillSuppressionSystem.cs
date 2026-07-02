// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology;

public sealed partial class SkillSuppressionSystem : EntitySystem
{
    [Dependency] private ExperienceSystem _experience = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkillSuppressionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SkillSuppressionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SkillSuppressionComponent> ent, ref ComponentStartup args)
    {
        if (!HasComp<ExperienceComponent>(ent))
            return;

        foreach (var tree in _prototype.EnumeratePrototypes<SkillTreePrototype>())
        {
            // remember any pre-existing override so cure restores it instead of wiping it
            ent.Comp.SavedOverrides[tree.ID] =
                _experience.TryGetOverrideSkillInfo(ent.Owner, tree.ID, out var previous) ? previous : null;

            _experience.TrySetOverrideSkill(ent.Owner, tree.ID, new SkillTreeInfo());
        }
    }

    private void OnShutdown(Entity<SkillSuppressionComponent> ent, ref ComponentShutdown args)
    {
        if (!HasComp<ExperienceComponent>(ent))
            return;

        foreach (var (tree, previous) in ent.Comp.SavedOverrides)
        {
            if (previous is null)
                _experience.TryRemoveOverrideSkill(ent.Owner, tree);
            else
                _experience.TrySetOverrideSkill(ent.Owner, tree, previous);
        }

        ent.Comp.SavedOverrides.Clear();
    }
}
