using Content.Server.GameTicking;
using Content.Server.SS220.Hallucination;
using Content.Shared.Eye;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Revenant.EntitySystems;

public sealed class CorporealSystem : SharedCorporealSystem
{
    [Dependency] private readonly HallucinationSystem _hallucination = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; // SS220 fix #3495

    public override void OnStartup(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        base.OnStartup(uid, component, args);

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibility);
        }

        //SS220-make-revenant-hallucinationSource-begin
        if (HasComp<HallucinationSourceComponent>(uid))
            _hallucination.SetHallucinationSourceActiveFlag(uid, true);
        //SS220-make-revenant-hallucinationSource-end
    }

    public override void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component, args);

        // SS220 fix #3495 begin
        // A revenant becomes spectral when corporeality expires, so it must not
        // stay trapped in a container that was closed during the effect.
        if (!TerminatingOrDeleted(uid))
            _container.TryRemoveFromContainer(uid, force: true);
        // SS220 fix #3495 end

        if (TryComp<VisibilityComponent>(uid, out var visibility) && _ticker.RunLevel != GameRunLevel.PostRound)
        {
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibility);
        }

        //SS220-make-revenant-hallucinationSource-begin
        if (HasComp<HallucinationSourceComponent>(uid))
            _hallucination.SetHallucinationSourceActiveFlag(uid, false);
        //SS220-make-revenant-hallucinationSource-end
    }
}
