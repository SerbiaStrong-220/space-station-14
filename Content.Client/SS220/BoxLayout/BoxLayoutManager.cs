// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Overlays;
using Content.Shared.SS220.Maths;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
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

    private BoxLayoutBoxesOverlayProvider _overlayProvider = default!;

    public EntityUid? Parent => _parent;
    private EntityUid? _parent;

    public Vector2? Point1 => _point1;
    private Vector2? _point1;

    public Vector2? Point2 => _point2;
    private Vector2? _point2;

    public Color Color => _color;
    private Color _color = Color.Green;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _input.UIKeyBindStateChanged += OnUIKeyBindStateChanged;

        var overlay = BoxesOverlay.GetOverlay();
        if (overlay.TryGetProvider<BoxLayoutBoxesOverlayProvider>(out var provider))
            _overlayProvider = provider;
        else
        {
            _overlayProvider = new BoxLayoutBoxesOverlayProvider();
            overlay.AddProvider(_overlayProvider);
        }
    }

    private bool OnUIKeyBindStateChanged(BoundKeyEventArgs args)
    {
        if (!Active || args.State != BoundKeyState.Down)
            return false;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            var handled = HandleUIClick();
            if (handled)
                args.Handle();

            return handled;
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            Cancel();
            args.Handle();
            return true;
        }
        else
            return false;
    }

    private bool HandleUIClick()
    {
        var mapCoords = GetMouseMapCoordinates();
        if (mapCoords.MapId == MapId.Nullspace)
            return false;

        var transform = _entity.System<TransformSystem>();
        if (_point1 == null)
        {
            EntityCoordinates coords;
            if (_map.TryFindGridAt(mapCoords, out var grid, out _))
                coords = transform.ToCoordinates(grid, mapCoords);
            else
                coords = transform.ToCoordinates(mapCoords);

            _parent = coords.EntityId;
            _point1 = coords.Position;
        }
        else if (_point2 == null && _parent != null)
        {
            var map = transform.GetMapId(_parent.Value);
            if (mapCoords.MapId != map)
            {
                var error = $"The coordinate was obtained from map {mapCoords.MapId}, when it should be from map {map}";
                DebugTools.Assert(error);

                _console.LocalShell.WriteError(error);
                return false;
            }

            var coords = transform.ToCoordinates(_parent.Value, mapCoords);
            _point2 = coords.Position;
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
        if (_point1 is not { } p1 || _point2 is not { } p2 || _parent is not { } parent)
            return null;

        var box = Box2.FromTwoPoints(p1, p2);
        if (box == Box2.Empty)
            return null;

        if (AttachToGrid)
        {
            var gridSize = 1f;
            if (_entity.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                gridSize = mapGrid.TileSize;

            MathHelperExtensions.AttachToLattice(ref box, gridSize);
        }

        return new BoxParams()
        {
            Parent = parent,
            Box = box
        };
    }

    public void StartNew()
    {
        if (Active)
            Cancel();
        else
            Clear();

        _active = true;
        Started?.Invoke();
    }

    public void Cancel()
    {
        Clear();
        _active = false;
        Cancelled?.Invoke();
    }

    private void Clear()
    {
        _point1 = null;
        _point2 = null;
        _parent = null;
        SetColor(null);
        AttachToGrid = false;
    }

    public void SetColor(Color? newColor)
    {
        newColor ??= DefaultColor;
        _color = newColor.Value;
    }

    public void SetOverlay(bool active)
    {
        _overlayProvider.Active = active;
    }

    public MapCoordinates GetMouseMapCoordinates()
    {
        return _eye.ScreenToMap(_input.MouseScreenPosition);
    }

    public struct BoxParams
    {
        public EntityUid Parent;
        public Box2 Box;
    }

    private sealed class BoxLayoutBoxesOverlayProvider() : BoxesOverlay.BoxesOverlayProvider()
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IBoxLayoutManager _boxLayoutManager = default!;

        public override List<BoxesOverlay.BoxOverlayData> GetBoxesDatas()
        {
            var list = new List<BoxesOverlay.BoxOverlayData>();
            if (!_boxLayoutManager.Active ||
                _boxLayoutManager.Point1 is not { } point1 ||
                _boxLayoutManager.Parent is not { } parent)
                return list;

            var transform = _entityManager.System<TransformSystem>();
            var mapCoords = _boxLayoutManager.GetMouseMapCoordinates();
            if (transform.GetMapId(parent) != mapCoords.MapId)
                return list;

            var point2 = transform.ToCoordinates(parent, mapCoords).Position;
            var box = Box2.FromTwoPoints(point1, point2);

            if (_boxLayoutManager.AttachToGrid)
            {
                var gridSize = 1f;
                if (_entityManager.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                    gridSize = mapGrid.TileSize;

                MathHelperExtensions.AttachToLattice(ref box, gridSize);
            }

            list.Add(new BoxesOverlay.BoxOverlayData(parent, box, _boxLayoutManager.Color.WithAlpha(0.5f)));

            return list;
        }
    }
}
