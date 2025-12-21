// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.Abilities.Mime;

namespace Content.Server.SS220.MimeRelic;

public sealed class MimeRelicSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MimeRelicComponent, ActivateInWorldEvent>(OnMimeRelicActivate);
    }

    private void OnMimeRelicActivate(Entity<MimeRelicComponent> ent, ref ActivateInWorldEvent args)
    {
        args.Handled = true;

        if (TryComp<MimePowersComponent>(args.User, out var mimePowersComponent) && mimePowersComponent.VowBroken)
        {
            _popupSystem.PopupEntity(Loc.GetString("mimeRelic-not-a-mime", ("user", args.User)), args.User);
            return;
        }

        if (_timing.CurTime < ent.Comp.TimeWallCanBePlaced)
            return;

        if (_container.IsEntityOrParentInContainer(args.User))
            return;

        var userTransform = Transform(args.User);
        var viewVector = userTransform.LocalRotation.ToWorldVec();
        var perpendToViewVector = new Vector2(viewVector.Y, -viewVector.X); // PerpendicularClockwise
        var centralWallPosition = userTransform.Coordinates.Offset(viewVector);

        if (CanPlaceWallInTile(centralWallPosition) == false)
        {
            _popupSystem.PopupEntity(Loc.GetString("mimeRelic-wall-failed", ("mime", args.User)), args.User);
            return;
        }

        PlaceWallInTile(centralWallPosition, ent.Comp.WallToPlacePrototype, ent.Comp.WallLifetime);
        ent.Comp.TimeWallCanBePlaced = _timing.CurTime + ent.Comp.CooldownTime;
        _popupSystem.PopupEntity(Loc.GetString("mimeRelic-wall-success", ("mime", args.User)), args.User);

        var orderList = new List<int>() { -1, 1 };
        foreach (var sideTileOrder in orderList)
            if (CanPlaceWallInTile(centralWallPosition.Offset(sideTileOrder * perpendToViewVector)))
                PlaceWallInTile(centralWallPosition.Offset(sideTileOrder * perpendToViewVector), ent.Comp.WallToPlacePrototype, ent.Comp.WallLifetime);
            else if (CanPlaceWallInTile(centralWallPosition.Offset(-2 * sideTileOrder * perpendToViewVector)))
                PlaceWallInTile(centralWallPosition.Offset(-2 * sideTileOrder * perpendToViewVector), ent.Comp.WallToPlacePrototype, ent.Comp.WallLifetime);
        // -2 is a magic number, whic gets neighbour tile opposite to central wall (oxCoo -> ooCox or ooCxo -> xoCoo)
    }

    private bool CanPlaceWallInTile(EntityCoordinates cordToPlace)
    {
        var tile = _turf.GetTileRef(cordToPlace.SnapToGrid());
        if (tile == null)
            return false;

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            return false;

        foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(tile.Value, 0f))
            if (HasComp<MobStateComponent>(entity) && HasComp<MimePowersComponent>(entity) == false) // fun with many mimes - checked
                return false;

        return true;
    }

    private void PlaceWallInTile(EntityCoordinates targetCord, string wallPrototype, TimeSpan wallLifetime)
    {
        var targetTile = _turf.GetTileRef(targetCord.SnapToGrid());

        if (CanPlaceWallInTile(targetCord) == false)
        {
            Log.Error("Error tried to place wall prototype, but tile is occupied");
            return;
        }

        if (targetTile == null) // useless if because it checked earlier...
            return;

        var placedWall = Spawn(wallPrototype, _turf.GetTileCenter(targetTile.Value));
        EnsureComp<TimedDespawnComponent>(placedWall, out var comp);
        comp.Lifetime = (float) wallLifetime.TotalSeconds;
    }
}
