using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Server.Abilities.Mime;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Mobs.Components;
using System.Numerics;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

//Make in alphabetic
namespace Content.Server.SS220.MimeRelic
{
    public sealed class MimeRelicSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        private TimeSpan _timeWallCanBePlaced = TimeSpan.Zero;
        public override void Initialize()
        {
            SubscribeLocalEvent<MimeRelicComponent, ActivateInWorldEvent>(OnMimeRelicActivate);
        }


        private void OnMimeRelicActivate(EntityUid uid, MimeRelicComponent component, ActivateInWorldEvent args)
        {
            args.Handled = true; // brainrot?
            if (HasComp<MimePowersComponent>(args.User) == false)
            {
                _popupSystem.PopupEntity(Loc.GetString("mimeRelic-not-a-mime"), args.User, args.User);
                return;
            }

            if (_timing.CurTime < _timeWallCanBePlaced)
                return; // sendmsgToPopup 'text-on-cooldown' // args.handled to start cd? It even had it?!

            if (_container.IsEntityOrParentInContainer(args.User))
                return;

            // check if that proto even exists.

            TransformComponent userTransform = Transform(args.User);
            Vector2 viewVector = userTransform.LocalRotation.ToWorldVec();
            Vector2 perpendToViewVector = new Vector2(viewVector.Y, -viewVector.X); // PerpendicularClockwise
            EntityCoordinates centralWallPosition = userTransform.Coordinates.Offset(viewVector);

            if (CanPlaceWallInTile(centralWallPosition) == false)
            {
                _popupSystem.PopupEntity(Loc.GetString("mimeRelic-wall-failed"), args.User, args.User);
                return;
            }
            PlaceWallInTile(centralWallPosition, component.WallToPlacePrototype,component.WallLifetime);
            _timeWallCanBePlaced = _timing.CurTime + component.CooldownTime;

            var orderList = new List<int>() { -1, 1 };
            foreach (int sideTileOrder in orderList)
                if (CanPlaceWallInTile(centralWallPosition.Offset(sideTileOrder * perpendToViewVector)))
                    PlaceWallInTile(centralWallPosition.Offset(sideTileOrder * perpendToViewVector), component.WallToPlacePrototype,component.WallLifetime);
            _popupSystem.PopupEntity(Loc.GetString("mimeRelic-wall-success"), args.User, args.User);
            //TODO additional walls if ZANAYTO *(-2)
            //
        }

        private bool CanPlaceWallInTile(EntityCoordinates cordToPlace)
        {
            TileRef? tile = cordToPlace.SnapToGrid().GetTileRef(EntityManager, _mapManager);
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
            TileRef? targetTile = targetCord.SnapToGrid().GetTileRef(EntityManager, _mapManager);
            if (CanPlaceWallInTile(targetCord) == false)
            {
                Log.Error("Error tried to place wall prototype, but tile is occupied");
                return;
            }
            if (targetTile == null) // useless if because it checked earlier...
                return;
            EntityUid placedWall = Spawn(wallPrototype, _turf.GetTileCenter(targetTile.Value));
            EnsureComp<TimedDespawnComponent>(placedWall, out TimedDespawnComponent comp);
            comp.Lifetime = (float) wallLifetime.TotalSeconds;
        }
    }
}