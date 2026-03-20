// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.SS220.Ipc;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.SS220.Ipc;

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
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent)
            || !
            humanoidAppearanceComponent.ClientOldMarkings.Markings.TryGetValue(MarkingToQuery, out var
            markings)
            || markings.Count <= MaximumMarkingCount)
            return;

            component.EnableAlternateHelmet = true;
    }
}
