using Content.Shared.SS220.Kpb;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.SS220.Kpb;

public sealed class SnoutHelmetSystem : EntitySystem
{
    private const MarkingCategories MarkingToQuery = MarkingCategories.Snout;
    private const int MaximumMarkingCount = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnoutHelmetComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, SnoutHelmetComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out HumanoidAppearanceComponent? humanoidAppearanceComponent) &&
            humanoidAppearanceComponent.ClientOldMarkings.Markings.TryGetValue(MarkingToQuery, out var markings) &&
            markings.Count > MaximumMarkingCount)
        {
            component.EnableAlternateHelmet = true;
        }
    }
}
