// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Rotation;
using Content.Shared.SS220.Photography;
using Content.Shared.StatusEffect;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private ISawmill _sawmill = Logger.GetSawmill("photo-visualizer");
    private Dictionary<string, PhotoVisualisation> _currentlyVisualized = new();
    private MapId? _reservedMap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PhotoReservedMapMessage>(OnMapReserved);
        InitializeCache();

        // Request a map, just in case it was already created
        // Needed for mid-round joins
        var ev = new PhotoRequestMapMessage();
        RaiseNetworkEvent(ev);
    }

    private void OnMapReserved(PhotoReservedMapMessage args)
    {
        _sawmill.Debug($"Reserved map received. MapId: {args.Map}");
        _reservedMap = args.Map;
    }

    public bool TryGetVisualization(string id, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (!_photoDataCache.TryGetValue(id, out var data))
        {
            eye = null;
            return false;
        }

        return TryGetVisualization(data, out eye);
    }

    public bool TryGetVisualization(PhotoData data, [NotNullWhen(true)] out EyeComponent? eye)
    {
        if (_currentlyVisualized.TryGetValue(data.Id, out var existingVisualization))
        {
            eye = existingVisualization.Eye;
            return true;
        }

        if (!_reservedMap.HasValue || !_mapMan.MapExists(_reservedMap.Value))
        {
            _sawmill.Debug($"Visualisation has not been loaded. Reason: No reserved map!");
            eye = null;
            return false;
        }

        var origin = new MapCoordinates(Vector2.Zero, _reservedMap.Value);
        var entities = new List<EntityUid>(data.Entities.Count + data.Grids.Count + 1);
        List<EntityUid> grids = new(data.Grids.Count);

        // Add grids
        for (var i = 0; i < data.Grids.Count; i++)
        {
            var gridDesc = data.Grids[i];
            var grid = _mapMan.CreateGrid(_reservedMap.Value);
            var gridEnt = grid.Owner;
            var gridXform = EnsureComp<TransformComponent>(gridEnt);
            entities.Add(gridEnt);
            grids.Add(gridEnt);

            _transform.SetParent(gridEnt, gridXform, _mapMan.GetMapEntityId(_reservedMap.Value));
            _transform.SetLocalRotationNoLerp(gridEnt, gridDesc.Rotation, gridXform);
            _transform.SetLocalPositionNoLerp(gridEnt, gridDesc.Position, gridXform);

            foreach (var (indices, tileType) in gridDesc.Tiles)
            {
                var tile = new Tile(tileType);
                _map.SetTile(gridEnt, grid, indices, tile);
            }

            // Handle decal state
            if (gridDesc.DecalGridState is not null)
            {
                var decalGrid = EnsureComp<DecalGridComponent>(gridEnt);
                var ev = new ComponentHandleState(gridDesc.DecalGridState, null);
                RaiseLocalEvent(gridEnt, ref ev);
            }
        }


        // Create and setup camera
        var camera = Spawn(null, origin);
        entities.Add(camera);

        var cameraXform = EnsureComp<TransformComponent>(camera);
        _transform.SetWorldPosition(cameraXform, data.CameraPosition);

        eye = EnsureComp<EyeComponent>(camera);
        _eye.SetZoom(camera, Vector2.One, eye);
        //Align with grid by subtracting grid angle (I have no idea why, but it works)
        _eye.SetRotation(camera, -data.CameraRotation, eye);


        // Add entities
        foreach (var entityDesc in data.Entities)
        {
            // Make sure entity transforms are parented
            EntityUid parent;

            if (entityDesc.GridIndex.HasValue && grids.TryGetValue(entityDesc.GridIndex.Value, out var maybeParent))
            {
                parent = maybeParent;
            }
            else if (_mapMan.GetMapEntityId(_reservedMap.Value) is { Valid: true } mapEnt)
            {
                parent = mapEnt;
            }
            else
            {
                continue;
            }

            EntityCoordinates coords = new(parent, entityDesc.Position);

            var entity = Spawn(entityDesc.PrototypeId, coords);
            entities.Add(entity);

            var xform = EnsureComp<TransformComponent>(entity);
            _transform.SetLocalRotationNoLerp(entity, entityDesc.Rotation, xform);

            if (TryComp<RotationVisualsComponent>(entity, out var rotationVisualsComp))
                rotationVisualsComp.AnimationTime = 0;

            // Handle appearance state
            if (entityDesc.Appearance is not null)
            {
                var appearanceComp = EnsureComp<AppearanceComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Appearance, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // TODO: deduplicate
            // Handle humanoid appearance state
            if (entityDesc.HumanoidAppearance is not null)
            {
                var humanoidAppearanceComp = EnsureComp<HumanoidAppearanceComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.HumanoidAppearance, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Handle point light state
            if (entityDesc.PointLight is not null)
            {
                var pointLightComp = EnsureComp<PointLightComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.PointLight, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Handle occluder state
            if (entityDesc.Occluder is not null)
            {
                var occluderComp = EnsureComp<OccluderComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Occluder, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Handle damageable state
            if (entityDesc.Damageable is not null)
            {
                var damageableComp = EnsureComp<DamageableComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Damageable, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Handle hands state
            if (entityDesc.Hands is not null)
            {
                var handsComp = EnsureComp<HandsComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.Hands, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Status Effects
            if (entityDesc.StatusEffects is not null)
            {
                var statusEffectsComp = EnsureComp<StatusEffectsComponent>(entity);
                var ev = new ComponentHandleState(entityDesc.StatusEffects, null);
                RaiseLocalEvent(entity, ref ev);
            }

            // Handle inventory
            if (entityDesc.Inventory is not null)
            {
                var inventoryComp = EnsureComp<InventoryComponent>(entity);
                foreach (var slotEntry in entityDesc.Inventory)
                {
                    if (_inventory.TryUnequip(entity, slotEntry.Key, out var item, true, true, false, inventoryComp))
                        QueueDel(item);
                    _inventory.SpawnItemInSlot(entity, slotEntry.Key, slotEntry.Value, true, true, inventoryComp);
                }
            }

            // Handle hands
            if (entityDesc.HandsContents is not null)
            {
                var handsComp = EnsureComp<HandsComponent>(entity);

                foreach (var handEntry in entityDesc.HandsContents)
                {
                    if (!_hands.TryGetHand(entity, handEntry.Key, out var hand, handsComp))
                        continue;

                    var inhandEntity = EntityManager.SpawnEntity(handEntry.Value, coords);
                    if (!_hands.TryPickup(entity, inhandEntity, hand, false, false, handsComp))
                        QueueDel(inhandEntity);
                }
            }
        }

        var photoVisDesc = new PhotoVisualisation(_reservedMap.Value, camera, origin, eye, entities, data.PhotoSize);
        _currentlyVisualized.Add(data.Id, photoVisDesc);
        _sawmill.Debug("Created map for visualization of the photo " + data.Id);

        return true;
    }

    public void DisposeVisualization(string id)
    {
        if (!_currentlyVisualized.TryGetValue(id, out var photoVisualisation))
            return;

        //_mapMan.DeleteMap(photoVisualisation.MapId);
        _currentlyVisualized.Remove(id);
        foreach (var entity in photoVisualisation.Entities)
        {
            EntityManager.DeleteEntity(entity);
        }
    }
}

public sealed class PhotoVisualisation
{
    public readonly MapId MapId;
    public readonly EntityUid Camera;
    public readonly EyeComponent Eye;
    public readonly MapCoordinates Origin;
    public readonly List<EntityUid> Entities;
    public readonly float Size;

    public PhotoVisualisation(MapId mapId, EntityUid camera, MapCoordinates origin, EyeComponent eye, List<EntityUid> entities, float size)
    {
        MapId = mapId;
        Origin = origin;
        Camera = camera;
        Eye = eye;
        Entities = entities;
        Size = size;
    }
}
