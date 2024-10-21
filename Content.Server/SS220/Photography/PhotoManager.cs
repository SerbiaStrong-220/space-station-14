// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Decals;
using Content.Server.Hands.Systems;
using Content.Server.IdentityManagement;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.SS220.Photography;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Photography;

public sealed class PhotoManager : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSys = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    private EntityQuery<MapGridComponent> _gridQuery = default!;
    private Dictionary<string, PhotoData> _photos = new();
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    private float _photoRange = 10; //10 is the radius of average powered light
    public const float MAX_PHOTO_RADIUS = 20;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();

        SubscribeNetworkEvent<PhotoDataRequest>(OnPhotoDataRequest);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _photos.Clear();
    }

    public void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _photos.Clear();
    }

    private void OnPhotoDataRequest(PhotoDataRequest message, EntitySessionEventArgs eventArgs)
    {
        var sender = eventArgs.SenderSession;

        if (!_photos.TryGetValue(message.Id, out var photoData))
        {
            _sawmill.Warning("Player " + sender.Name + " requested data of a photo with ID " + message.Id + " but it doesn't exist!");
            photoData = new PhotoData(message.Id, 3, Vector2.Zero, 0, false);
        }

        var ev = new PhotoDataRequestResponse(photoData, message.Id);
        RaiseNetworkEvent(ev, sender);
    }

    public bool TryCapture(
        MapCoordinates focusCoords,
        Angle cameraRotation,
        float captureSize,
        [NotNullWhen(true)] out string? id,
        [NotNullWhen(true)] out List<string>? seenObjects)
    {
        id = Guid.NewGuid().ToString();
        seenObjects = new();
        var focusWorldPos = focusCoords.Position;
        var captureSizeSquareHalf = (captureSize * captureSize) / 2; //for optimization purposes

        var radius = MathF.Min(_photoRange, MAX_PHOTO_RADIUS); //cap because scary
        var range = new Vector2(radius, radius);
        var worldArea = new Box2(focusWorldPos - range, focusWorldPos + range);

        var data = new PhotoData(id, captureSize, focusWorldPos, cameraRotation);

        // Get grids in range
        var intersectingGrids = _mapMan.FindGridsIntersecting(focusCoords.MapId, worldArea);
        Dictionary<EntityUid, int> gridIdMap = new();

        foreach (var grid in intersectingGrids)
        {
            var gridUid = grid.Owner;

            if (!TryComp<TransformComponent>(gridUid, out var gridXform))
                continue;

            var gridPosRot = _transform.GetWorldPositionRotation(gridXform);
            var gridData = new PhotoGridData(gridPosRot.WorldPosition, gridPosRot.WorldRotation);
            foreach (var tile in _mapSys.GetTilesIntersecting(gridUid, grid, worldArea, true))
            {
                var indices = tile.GridIndices;
                var tileType = tile.Tile.TypeId;
                gridData.Tiles.Add((indices, tileType));
            }

            if (TryComp<DecalGridComponent>(gridUid, out var decalGrid))
            {
                var (_, _, matrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
                var localPos = Vector2.Transform(focusWorldPos, matrix);
                var localAABB = new Box2(localPos - range, localPos + range);
                var chunkDict = new Dictionary<Vector2i, DecalGridComponent.DecalChunk>();
                var chunks = new ChunkIndicesEnumerator(localAABB, DecalSystem.ChunkSize);
                var chunkCollection = decalGrid.ChunkCollection.ChunkCollection;

                while (chunks.MoveNext(out var chunkOrigin))
                {
                    if (!chunkCollection.TryGetValue(chunkOrigin.Value, out var chunk))
                        continue;

                    chunkDict.Add(chunkOrigin.Value, chunk);
                }

                gridData.DecalGridState = new DecalGridState(chunkDict);
            }

            gridIdMap.Add(gridUid, data.Grids.Count);
            data.Grids.Add(gridData);
        }

        // Get entities in range
        foreach (var entity in _entityLookup.GetEntitiesInRange(focusCoords, radius, LookupFlags.Uncontained))
        {
            var protoId = MetaData(entity).EntityPrototype?.ID;
            if (protoId is null)
                continue;

            // No grids here
            if (_gridQuery.HasComponent(entity))
                continue;

            if (!TryComp<TransformComponent>(entity, out var entXform))
                continue;

            Vector2 position;
            Angle rotation;
            int? gridKey = null;

            if (entXform.GridUid is { } gridUid && gridIdMap.TryGetValue(gridUid, out var gridKeyMaybe))
            {
                gridKey = gridKeyMaybe;
                position = entXform.LocalPosition;
                rotation = entXform.LocalRotation;
            }
            else
                (position, rotation) = _transform.GetWorldPositionRotation(entXform);

            // TODO: deduplicate
            // Appearance state
            AppearanceComponentState? appearanceState = null;
            if (TryComp<AppearanceComponent>(entity, out var appearance))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero);
                if (maybe_state is AppearanceComponentState state)
                {
                    appearanceState = new AppearanceComponentState(state.Data.ShallowClone());
                }
            }

            // Humanoid appearance state
            IComponentState? humanoidAppearanceState = null;
            if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoidAppearance))
            {
                humanoidAppearanceState = EntityManager.GetComponentState(EntityManager.EventBus, humanoidAppearance, null, GameTick.Zero);

                if ((_transform.GetWorldPosition(entity) - focusWorldPos).LengthSquared() < captureSizeSquareHalf)
                {
                    var identityEnt = Identity.Entity(entity, EntityManager);
                    seenObjects.Add(Name(identityEnt));
                }
            }

            // Point light state
            PointLightComponentState? pointLightState = null;
            if (TryComp<PointLightComponent>(entity, out var pointLight))
            {
                // not networked, have to do it like this otherwise crashes in debug
                pointLightState = new PointLightComponentState()
                {
                    Color = pointLight.Color,
                    Energy = pointLight.Energy,
                    Softness = pointLight.Softness,
                    CastShadows = pointLight.CastShadows,
                    Enabled = pointLight.Enabled,
                    Radius = pointLight.Radius,
                    Offset = pointLight.Offset
                };
            }

            // Occluder state
            OccluderComponent.OccluderComponentState? occluderState = null;
            if (TryComp<OccluderComponent>(entity, out var occluder))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, occluder, null, GameTick.Zero);
                if (maybe_state is OccluderComponent.OccluderComponentState state)
                {
                    occluderState = state;
                }
            }

            // Damageable state
            DamageableComponentState? damageableState = null;
            if (TryComp<DamageableComponent>(entity, out var damageable))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, damageable, null, GameTick.Zero);
                if (maybe_state is DamageableComponentState state)
                {
                    damageableState = state;
                }
            }

            // Hands state
            HandsComponentState? handsState = null;
            if (TryComp<HandsComponent>(entity, out var handsComp))
            {
                var maybe_state = EntityManager.GetComponentState(EntityManager.EventBus, handsComp, null, GameTick.Zero);
                if (maybe_state is HandsComponentState state)
                {
                    handsState = state;
                }
            }

            // Inventory & Hands
            Dictionary<string, string>? inventory = null;
            Dictionary<string, string>? hands = null;

            var haveHands = handsComp != null;
            if (haveHands)
                hands = new();

            var haveInventory = TryComp(entity, out InventoryComponent? inventoryComp);
            if (haveInventory)
                inventory = new();

            if (haveHands || haveInventory)
            {
                foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
                {
                    var proto = MetaData(item).EntityPrototype?.ID;
                    if (proto is null)
                        continue;

                    if (haveInventory && _inventory.TryGetContainingSlot(item, out var slot))
                    {
                        inventory!.Add(slot.Name, proto);
                    }
                    else if (haveHands && _hands.IsHolding(entity, item, out var hand, handsComp))
                    {
                        foreach (var handEntry in handsComp!.Hands)
                        {
                            if (handEntry.Value != hand)
                                continue;

                            hands!.Add(handEntry.Key, proto);
                            break;
                        }
                    }
                }
            }

            var ent_data = new PhotoEntityData(protoId, position, rotation)
            {
                GridIndex = gridKey,
                Appearance = appearanceState,
                HumanoidAppearance = humanoidAppearanceState,
                PointLight = pointLightState,
                Occluder = occluderState,
                Damageable = damageableState,
                Hands = handsState,
                Inventory = inventory,
                HandsContents = hands
            };
            data.Entities.Add(ent_data);
        }

        _photos.Add(id, data);
        _sawmill.Debug("Photo taken! Entity count: " + data.Entities.Count + ", Grid count: " + data.Grids.Count + ", ID: " + id);

        return true;
    }
}
