
using Content.Client.SS220.Overlays;
using Content.Shared.SS220.Maths;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.SS220.BoxLayout;

public sealed class BoxLayoutManager : IBoxLayoutManager
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

    public static Color DefaultColor = Color.Green;

    public event Action? Started;
    public event Action<BoxParams>? Ended;
    public event Action? Cancelled;

    public bool Active => _active;
    private bool _active;

    public bool AttachToGrid
    {
        get => _attachToGrid;
        set => _attachToGrid = value;
    }
    private bool _attachToGrid;

    public BoxParams? CurParams => GetBoxParams();

    private BoxesOverlay? _overlay;
    private BoxLayoutBoxesOverlayProvider _overlayProvider = default!;

    internal EntityUid? Parent;
    internal Vector2? Point1;
    internal Vector2? Point2;
    internal Color Color = Color.Green;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _input.UIKeyBindStateChanged += OnUIKeyBindStateChanged;
        _overlay = BoxesOverlay.GetOverlay();
        _overlayProvider = new BoxLayoutBoxesOverlayProvider(this);
    }

    private bool OnUIKeyBindStateChanged(BoundKeyEventArgs args)
    {
        if (!Active || args.State != BoundKeyState.Down)
            return false;

        if (args.Function == EngineKeyFunctions.UIClick)
            return OnLeftClick();
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            Cancel();
            return true;
        }
        else
            return false;
    }

    private bool OnLeftClick()
    {
        var mapCoords = GetMouseMapCoordinates();
        if (mapCoords.MapId == MapId.Nullspace)
            return false;

        var transform = _entity.System<TransformSystem>();
        if (Point1 == null)
        {
            EntityCoordinates coords;
            if (_map.TryFindGridAt(mapCoords, out var grid, out _))
                coords = transform.ToCoordinates(grid, mapCoords);
            else
                coords = transform.ToCoordinates(mapCoords);

            Parent = coords.EntityId;
            Point1 = coords.Position;
        }
        else if (Point2 == null && Parent != null)
        {
            var map = transform.GetMapId(Parent.Value);
            if (mapCoords.MapId != map)
            {
                var error = $"The coordinate was obtained from map {mapCoords.MapId}, when it should be from map {map}";
#if DEBUG
                throw new Exception(error);
#endif
                _console.LocalShell.WriteError(error);
                return false;
            }

            var coords = transform.ToCoordinates(Parent.Value, mapCoords);
            Point2 = coords.Position;
        }
        BoxEndedCheck();

        return true;
    }

    private void BoxEndedCheck()
    {
        var @params = GetBoxParams();
        if (@params != null)
        {
            Ended?.Invoke(@params.Value);
            _active = false;
        }
    }

    public BoxParams? GetBoxParams()
    {
        if (Point1 is not { } p1 || Point2 is not { } p2 || Parent is not { } parent)
            return null;

        var box = Box2.FromTwoPoints(p1, p2);
        if (box == Box2.Empty)
            return null;

        if (AttachToGrid)
        {
            var gridSize = 1f;
            if (_entity.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                gridSize = mapGrid.TileSize;

            MathHelperExtensions.AttachToGrid(ref box, gridSize);
        }

        return new BoxParams()
        {
            Parent = _entity.GetNetEntity(parent),
            Box = box
        };
    }

    public void Cancel()
    {
        Clear();
        _active = false;
        Cancelled?.Invoke();
    }

    public void StartNewBox()
    {
        if (Active)
            Cancel();
        else
            Clear();

        _active = true;
        Started?.Invoke();
    }

    private void Clear()
    {
        Point1 = null;
        Point2 = null;
        Parent = null;
        SetColor(null);
        AttachToGrid = false;
    }

    public void SetColor(Color? newColor)
    {
        newColor ??= DefaultColor;
        Color = newColor.Value;
    }

    public void SetOverlay(bool enabled)
    {
        if (enabled)
            _overlay?.AddProvider(_overlayProvider);
        else
            _overlay?.RemoveProvider(_overlayProvider);
    }

    public MapCoordinates GetMouseMapCoordinates()
    {
        return _eye.ScreenToMap(_input.MouseScreenPosition);
    }

    public struct BoxParams
    {
        public NetEntity Parent;
        public Box2 Box;
    }

    private sealed class BoxLayoutBoxesOverlayProvider(BoxLayoutManager layoutManager) : BoxesOverlay.BoxesOverlayProvider()
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly BoxLayoutManager _layoutManager = layoutManager;

        public override List<BoxesOverlay.BoxOverlayData> GetBoxesDatas()
        {
            var list = new List<BoxesOverlay.BoxOverlayData>();
            if (!_layoutManager.Active ||
                _layoutManager.Point1 is not { } point1 ||
                _layoutManager.Parent is not { } parent)
                return list;

            var transform = _entityManager.System<TransformSystem>();
            var mapCoords = _layoutManager.GetMouseMapCoordinates();
            if (transform.GetMapId(parent) != mapCoords.MapId)
                return list;

            var point2 = transform.ToCoordinates(parent, mapCoords).Position;
            var box = Box2.FromTwoPoints(point1, point2);

            if (_layoutManager.AttachToGrid)
            {
                var gridSize = 1f;
                if (_entityManager.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                    gridSize = mapGrid.TileSize;

                MathHelperExtensions.AttachToGrid(ref box, gridSize);
            }

            list.Add(new BoxesOverlay.BoxOverlayData(parent, box, _layoutManager.Color.WithAlpha(0.5f)));

            return list;
        }
    }
}
