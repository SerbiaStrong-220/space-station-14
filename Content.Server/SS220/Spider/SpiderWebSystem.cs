// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Spider;
using Content.Shared.SS220.Atmos;
using Content.Shared.Whitelist;
using Content.Server.Atmos.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Spider;

public sealed class SpiderWebSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BarotraumaComponent, BarotraumaDamageAttemptEvent>(OnBarotraumaDamageAttempt);
    }

    /// <summary>
    /// Checks whether the specified tile contains an object with the <see cref="SpiderWebObjectComponent"/> component.
    /// </summary>
    public bool IsTileBlockedByWeb(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return false;

        var anchored = _mapSystem.GetAnchoredEntities((gridUid.Value, gridComp), coords);
        foreach (var ent in anchored)
        {
            if (HasComp<SpiderWebObjectComponent>(ent))
                return true;
        }
        return false;
    }

    public bool IsTileProtectedFromBarotraumaByWeb(EntityUid uid, EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return false;

        var anchored = _mapSystem.GetAnchoredEntities((gridUid.Value, gridComp), coords);
        foreach (var ent in anchored)
        {
            if (!TryComp<SpiderWebObjectComponent>(ent, out var web))
                continue;

            if (_whitelist.IsWhitelistPass(web.BarotraumaImmuneWhitelist, uid))
                return true;
        }

        return false;
    }

    private void OnBarotraumaDamageAttempt(Entity<BarotraumaComponent> ent, ref BarotraumaDamageAttemptEvent args)
    {
        if (IsTileProtectedFromBarotraumaByWeb(ent, Transform(ent).Coordinates))
            args.Cancel();
    }
}
