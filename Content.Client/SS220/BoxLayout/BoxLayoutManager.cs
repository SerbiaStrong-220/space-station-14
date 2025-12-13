// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Overlays;
using Content.Shared.SS220.Input;
using Content.Shared.SS220.Maths;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.SS220.BoxLayout;

public sealed class BoxLayoutManager : IBoxLayoutManager
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

    public static readonly Color DefaultColor = Color.Green;

    public event Action? Started;
    public event Action<BoxArgs>? Ended;
    public event Action? Cancelled;

    public bool Active => _active;
    private bool _active = false;

    public bool AttachToLattice
    {
        get => _attachToGrid;
        set => _attachToGrid = value;
    }
    private bool _attachToGrid;

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

        var overlay = BoxesOverlay.GetOverlay();
        _overlayProvider = overlay.EnsureProvider<BoxLayoutBoxesOverlayProvider>();
    }

    private void RegisterInput()
    {
        CommandBinds.Builder
            .Bind(KeyFunctions220.BoxLayoutSetPoint, new PointerInputCmdHandler(
                (session, coords, target) =>
                {
                    if (!Active)
                        return false;

                    if (_point1 == null)
                    {
                        // parent is determined by the first point
                        _parent = coords.EntityId;
                        _point1 = coords.Position;
                        return true;
                    }
                    else if (_point2 == null && _parent != null)
                    {
                        var transform = _entity.System<TransformSystem>();
                        var mapCoords = transform.ToMapCoordinates(coords);

                        var map = transform.GetMapId(_parent.Value);
                        if (mapCoords.MapId != map)
                        {
                            var error = $"Expected coordinates from map {map}, but was {mapCoords.MapId}";
                            DebugTools.Assert(error);
                            _console.LocalShell.WriteError(error);

                            return false;
                        }

                        var localCoords = transform.ToCoordinates(_parent.Value, mapCoords);
                        _point2 = localCoords.Position;
                    }

                    BoxEndedCheck();
                    return true;
                }, ignoreUp: true))
            .Bind(KeyFunctions220.BoxLayoutCancel, InputCmdHandler.FromDelegate(
                session =>
                {
                    if (!Active)
                        return;

                    Cancel();
                }))
            .Register<BoxLayoutManager>();
    }

    private void UnregisterInput()
    {
        CommandBinds.Unregister<BoxLayoutManager>();
    }

    private void BoxEndedCheck()
    {
        if (TryGetBoxArgs(out var args))
        {
            Ended?.Invoke(args.Value);
            SetActive(false);
        }
    }

    public bool TryGetBoxArgs([NotNullWhen(true)] out BoxArgs? args)
    {
        args = null;
        if (_parent is not { } parent
            || _point1 is not { } p1
            || _point2 is not { } p2)
            return false;

        var box = Box2.FromTwoPoints(p1, p2);
        if (AttachToLattice)
        {
            var gridSize = 1f;
            if (_entity.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                gridSize = mapGrid.TileSize;

            MathHelperExtensions.AttachToLattice(ref box, gridSize);
        }

        args = new BoxArgs(parent, box);
        return true;
    }

    public void StartNew()
    {
        if (Active)
            Cancel();
        else
            Clear();

        SetActive(true);
        Started?.Invoke();
    }

    public void Cancel()
    {
        Clear();
        SetActive(false);
        Cancelled?.Invoke();
    }

    private void SetActive(bool active)
    {
        if (_active == active)
            return;

        if (active)
        {
            _input.Contexts.SetActiveContext("editor");
            RegisterInput();
        }
        else
        {
            _entity.System<InputSystem>().SetEntityContextActive();
            UnregisterInput();
        }

        _active = active;
    }

    private void Clear()
    {
        _point1 = null;
        _point2 = null;
        _parent = null;
        SetColor(null);
        AttachToLattice = false;
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

    public struct BoxArgs(EntityUid parent, Box2 box)
    {
        public EntityUid Parent = parent;
        public Box2 Box = box;
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

            if (_boxLayoutManager.AttachToLattice)
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
