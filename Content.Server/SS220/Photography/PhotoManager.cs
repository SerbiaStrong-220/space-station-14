// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Decals;
using Content.Server.Hands.Systems;
using Content.Server.Players.RateLimiting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Players.RateLimiting;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Photography;
using Content.Shared.SSDIndicator;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Server.SS220.Photography;

public sealed partial class PhotoManager : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private PlayerRateLimitManager _rateLimit = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSys = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private EntityQuery<MapGridComponent> _gridQuery = default!;
    private Dictionary<string, PhotoData> _photos = new();
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    public const string RequestRateLimitKey = "PhotoRequest";

    private float _photoRange = 10; //10 is the radius of average powered light
    public const float MAX_PHOTO_RADIUS = 20;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
        _rateLimit.Register(RequestRateLimitKey,
            new RateLimitRegistration(CCVars220.PhotoRequestRateLimitPeriod, CCVars220.PhotoRequestRateLimitCount, null));

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

        // Count before doing lookups, serializing data, or reporting invalid requests.
        if (_rateLimit.CountAction(sender, RequestRateLimitKey) != RateLimitStatus.Allowed)
            return;

        if (sender.AttachedEntity is not { } actor ||
            !TryGetEntity(message.Photo, out var photo) || photo is not { } photoUid ||
            !TryComp<PhotoComponent>(photoUid, out var component) ||
            component.PhotoID != message.Id ||
            !_ui.IsUiOpen(photoUid, PhotoUiKey.Key, actor) ||
            !_photos.TryGetValue(component.PhotoID, out var photoData))
        {
            RaiseNetworkEvent(new PhotoDataRequestResponse(null, message.Id), sender);
            return;
        }

        RaiseNetworkEvent(new PhotoDataRequestResponse(photoData, message.Id), sender);
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

            var gridXform = Transform(gridUid);

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
                var localAABB = _transform.GetInvWorldMatrix(gridUid).TransformBox(worldArea);
                var chunkDict = new Dictionary<Vector2i, DecalGridComponent.DecalChunk>();
                var chunks = new ChunkIndicesEnumerator(localAABB, DecalSystem.ChunkSize);
                var chunkCollection = decalGrid.ChunkCollection.ChunkCollection;

                while (chunks.MoveNext(out var chunkOrigin))
                {
                    if (!chunkCollection.TryGetValue(chunkOrigin.Value, out var chunk))
                        continue;

                    var decals = new Dictionary<uint, Decal>();
                    foreach (var (decalId, decal) in chunk.Decals)
                    {
                        if (localAABB.Contains(decal.Coordinates))
                            decals.Add(decalId, decal);
                    }

                    if (decals.Count > 0)
                        chunkDict.Add(chunkOrigin.Value, new DecalGridComponent.DecalChunk(decals));
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

            var entXform = Transform(entity);

            Vector2 position;
            Angle rotation;
            int? gridKey = null;

            if (entXform.GridUid is { } gridUid && gridIdMap.TryGetValue(gridUid, out var gridKeyMaybe))
            {
                gridKey = gridKeyMaybe;
                var worldPosition = _transform.GetWorldPosition(entXform);
                position = Vector2.Transform(worldPosition, _transform.GetInvWorldMatrix(gridUid));
                rotation = _transform.GetWorldRotation(entXform) - _transform.GetWorldRotation(gridUid);
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
            IComponentState? humanoidProfileState = null;
            if (TryComp<HumanoidProfileComponent>(entity, out var humanoidProfile))
            {
                humanoidProfileState = EntityManager.GetComponentState(EntityManager.EventBus, humanoidProfile, null, GameTick.Zero);

                if ((_transform.GetWorldPosition(entity) - focusWorldPos).LengthSquared() < captureSizeSquareHalf)
                {
                    seenObjects.Add(Identity.Name(entity, EntityManager));
                }
            }

            IComponentState? ssdIndicatorState = null;
            if (TryComp<SSDIndicatorComponent>(entity, out var ssdIndicator))
                ssdIndicatorState = EntityManager.GetComponentState(EntityManager.EventBus, ssdIndicator, null, GameTick.Zero);

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
                    damageableState = new DamageableComponentState(state.Damage.Clone(),
                        state.DamageContainerId, state.ModifierSetId, state.HealthBarThreshold);
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
                    else if (haveHands && _hands.IsHolding((entity, handsComp), item, out var hand))
                    {
                        hands!.Add(hand, proto);
                    }
                }
            }

            var ent_data = new PhotoEntityData(protoId, position, rotation)
            {
                GridIndex = gridKey,
                Appearance = appearanceState,
                HumanoidProfile = humanoidProfileState,
                SsdIndicator = ssdIndicatorState,
                Organs = CaptureOrgans(entity),
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

    private List<PhotoOrganData>? CaptureOrgans(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body) || body.Organs == null)
            return null;

        var organs = new List<PhotoOrganData>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (MetaData(organ).EntityPrototype is not { } prototype ||
                !TryComp<VisualOrganComponent>(organ, out var visual))
                continue;

            var visualState = new VisualOrganComponent.VisualOrganComponent_AutoState
            {
                Data = _serialization.CreateCopy(visual.Data, notNullableOverride: true),
                Profile = visual.Profile
            };

            VisualOrganMarkingsComponent.VisualOrganMarkingsComponent_AutoState? markingsState = null;
            if (TryComp<VisualOrganMarkingsComponent>(organ, out var markings))
            {
                markingsState = new VisualOrganMarkingsComponent.VisualOrganMarkingsComponent_AutoState
                {
                    Markings = _serialization.CreateCopy(markings.Markings, notNullableOverride: true),
                    HideableLayers = _serialization.CreateCopy(markings.HideableLayers, notNullableOverride: true),
                    DependentHidingLayers = _serialization.CreateCopy(markings.DependentHidingLayers, notNullableOverride: true),
                    MarkingsDisplacement = _serialization.CreateCopy(markings.MarkingsDisplacement, notNullableOverride: true)
                };
            }

            // Network states are not DataDefinitions: copy their mutable payloads, not the states themselves.
            organs.Add(new PhotoOrganData(prototype.ID, visualState, markingsState));
        }

        return organs;
    }
}
