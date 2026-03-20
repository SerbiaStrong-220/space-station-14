// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Spider;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Spider;

public sealed class SpiderWebSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

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
}
