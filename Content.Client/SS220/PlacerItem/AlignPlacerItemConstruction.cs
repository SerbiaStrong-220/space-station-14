// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Gameplay;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.RCD.Systems;
using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.SS220.PlacerItem.Components;
using Content.Shared.SS220.PlacerItem.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.SS220.PlacerItem;

public sealed class AlignPlacerItemConstruction : PlacementMode
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private readonly RCDSystem _rcdSystem;
    private readonly PlacerItemSystem _placerItemSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedMapSystem _mapSystem;

    private const float SearchBoxSize = 2f;
    private const float PlaceColorBaseAlpha = 0.5f;

    private EntityCoordinates _unalignedMouseCoords = default;

    public AlignPlacerItemConstruction(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);
        _rcdSystem = _entityManager.System<RCDSystem>();
        _placerItemSystem = _entityManager.System<PlacerItemSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _mapSystem = _entityManager.System<SharedMapSystem>();

        ValidPlaceColor = ValidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
    }

    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
        MouseCoords = _unalignedMouseCoords.AlignWithClosestGridTile(SearchBoxSize, _entityManager, _mapManager);

        var gridId = _transformSystem.GetGrid(MouseCoords);
        if (!_entityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
            return;

        CurrentTile = _mapSystem.GetTileRef(gridId.Value, mapGrid, MouseCoords);

        float tileSize = mapGrid.TileSize;
        GridDistancing = tileSize;

        if (pManager.CurrentPermission!.IsTile)
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2,
                CurrentTile.Y + tileSize / 2));
        }
        else
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
        }
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        // If the destination is out of interaction range, set the placer alpha to zero
        if (!_entityManager.TryGetComponent<TransformComponent>(player, out var xform))
            return false;

        if (!_transformSystem.InRange(xform.Coordinates, position, SharedInteractionSystem.InteractionRange))
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(0);
            return false;
        }
        // Otherwise restore the alpha value
        else
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
        }

        if (!_entityManager.TryGetComponent<HandsComponent>(player, out var hands))
            return false;

        var heldEntity = hands.ActiveHand?.HeldEntity;

        if (!_entityManager.TryGetComponent<PlacerItemComponent>(heldEntity, out var placerItemComp))
            return false;

        var gridUid = _transformSystem.GetGrid(position);
        if (!_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var mapGrid))
            return false;

        var posVector = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, position);

        // Determine if the user is hovering over a target
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
            return false;

        var target = screen.GetClickedEntity(_transformSystem.ToMapCoordinates(_unalignedMouseCoords));

        return _placerItemSystem.IsPlacementOperationStillValid((heldEntity.Value, placerItemComp), (gridUid.Value, mapGrid), posVector, target, player.Value);
    }
}
