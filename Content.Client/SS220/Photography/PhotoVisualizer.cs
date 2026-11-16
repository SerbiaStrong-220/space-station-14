// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Content.Shared.Damage.Components;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Rotation;
using Content.Shared.SS220.Photography;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusEffect;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private const int TileBatchSize = 32;
    private readonly List<EntityUid> _sceneEntities = new();
    private readonly List<EntityUid> _sceneGrids = new();
    private readonly List<(Vector2i, Tile)> _sceneTiles = new(TileBatchSize);
    private EntityQuery<RotationVisualsComponent> _rotationVisualsQuery;
    private EyeComponent? _sceneEye;
    private readonly Queue<EntityUid> _cleanupEntities = new();
    private MapId? _reservedMap;

    public override void Initialize()
    {
        base.Initialize();

        _rotationVisualsQuery = GetEntityQuery<RotationVisualsComponent>();
        SubscribeNetworkEvent<PhotoReservedMapMessage>(OnMapReserved);
        InitializeCache();

        // Mid-round joins may have missed the map announcement.
        RaiseNetworkEvent(new PhotoRequestMapMessage());
    }

    private void OnMapReserved(PhotoReservedMapMessage args)
    {
        _reservedMap = args.Map;
    }

    /// <summary>
    /// Reconstruct a server-authored snapshot in small steps, never a whole scene in one frame.
    /// The caller owns the enumerator and must dispose it before cleaning up the scene.
    /// </summary>
    private IEnumerator<bool> CreateVisualization(PhotoData data, MapId mapId)
    {
        var origin = new MapCoordinates(Vector2.Zero, mapId);
        var entities = _sceneEntities;
        var grids = _sceneGrids;
        var tiles = _sceneTiles;

        var camera = Spawn(null, origin);
        entities.Add(camera);
        var cameraXform = Transform(camera);
        _transform.SetWorldPosition(cameraXform, data.CameraPosition);
        var eye = EnsureComp<EyeComponent>(camera);
        _eye.SetZoom(camera, Vector2.One, eye);
        _eye.SetRotation(camera, -data.CameraRotation, eye);
        _sceneEye = eye;
        yield return true;

        foreach (var gridDesc in data.Grids)
        {
            var grid = _mapMan.CreateGrid(mapId);
            var gridEnt = grid.Owner;
            var gridXform = Transform(gridEnt);
            entities.Add(gridEnt);
            grids.Add(gridEnt);

            _transform.SetLocalRotationNoLerp(gridEnt, gridDesc.Rotation, gridXform);
            _transform.SetLocalPositionNoLerp(gridEnt, gridDesc.Position, gridXform);
            yield return true;

            foreach (var (indices, tileType) in gridDesc.Tiles)
            {
                tiles.Add((indices, new Tile(tileType)));
                if (tiles.Count < TileBatchSize)
                    continue;

                _map.SetTiles(gridEnt, grid, tiles);
                tiles.Clear();
                yield return true;
            }

            if (tiles.Count > 0)
            {
                _map.SetTiles(gridEnt, grid, tiles);
                tiles.Clear();
                yield return true;
            }

            if (gridDesc.DecalGridState is not null)
            {
                var decalGrid = EnsureComp<DecalGridComponent>(gridEnt);
                var ev = new ComponentHandleState(gridDesc.DecalGridState, null);
                EntityManager.EventBus.RaiseComponentEvent(gridEnt, decalGrid, ref ev);
                yield return true;
            }
        }

        foreach (var entityDesc in data.Entities)
        {
            // Make sure entity transforms are parented
            EntityUid parent;

            if (entityDesc.GridIndex.HasValue && grids.TryGetValue(entityDesc.GridIndex.Value, out var maybeParent))
            {
                parent = maybeParent;
            }
            else if (_mapMan.GetMapEntityId(mapId) is { Valid: true } mapEnt)
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

            if (_rotationVisualsQuery.TryComp(entity, out var rotationVisualsComp))
                rotationVisualsComp.AnimationTime = 0;

            ApplyState<AppearanceComponent>(entity, entityDesc.Appearance);
            ApplyState<HumanoidProfileComponent>(entity, entityDesc.HumanoidProfile);
            ApplyState<SSDIndicatorComponent>(entity, entityDesc.SsdIndicator);
            ApplyState<PointLightComponent>(entity, entityDesc.PointLight);
            ApplyState<OccluderComponent>(entity, entityDesc.Occluder);
            ApplyState<DamageableComponent>(entity, entityDesc.Damageable);
            ApplyState<HandsComponent>(entity, entityDesc.Hands);
            ApplyState<StatusEffectsComponent>(entity, entityDesc.StatusEffects);
            yield return true;

            if (entityDesc.Organs is { } organs)
            {
                var container = _containers.EnsureContainer<Container>(entity, BodyComponent.ContainerID);
                foreach (var organDesc in organs)
                {
                    var organ = Spawn(organDesc.Prototype, coords);
                    entities.Add(organ);
                    ApplyState<VisualOrganComponent>(organ, organDesc.Visual);
                    ApplyState<VisualOrganMarkingsComponent>(organ, organDesc.Markings);
                    _containers.Insert(organ, container);
                    yield return true;
                }
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
                    if (_inventory.TryGetSlotEntity(entity, slotEntry.Key, out var equipped, inventoryComp))
                        entities.Add(equipped.Value);
                    yield return true;
                }
            }

            // Handle hands
            if (entityDesc.HandsContents is not null)
            {
                var handsComp = EnsureComp<HandsComponent>(entity);

                foreach (var handEntry in entityDesc.HandsContents)
                {
                    if (!_hands.TryGetHand((entity, handsComp), handEntry.Key, out _))
                        continue;

                    var inhandEntity = Spawn(handEntry.Value, coords);
                    entities.Add(inhandEntity);
                    if (!_hands.TryPickup(entity, inhandEntity, handEntry.Key, checkActionBlocker: false,
                            animate: false, handsComp: handsComp))
                        QueueDel(inhandEntity);
                    yield return true;
                }
            }
            yield return true;
        }
    }

    private void ApplyState<T>(EntityUid entity, IComponentState? state) where T : Component, new()
    {
        if (state == null)
            return;

        var component = EnsureComp<T>(entity);
        var ev = new ComponentHandleState(state, null);
        EntityManager.EventBus.RaiseComponentEvent(entity, component, ref ev);
    }

    private void DisposeVisualization()
    {
        // Children first: deleting a populated grid would otherwise delete the whole scene at once.
        for (var i = _sceneEntities.Count - 1; i >= 0; i--)
        {
            _cleanupEntities.Enqueue(_sceneEntities[i]);
        }

        _sceneEye = null;
        _sceneEntities.Clear();
        _sceneGrids.Clear();
        _sceneTiles.Clear();
    }
}
