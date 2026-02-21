// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Overlays;
using Content.Shared.SS220.Input;
using Content.Shared.SS220.Maths;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Console;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Client.SS220.BoxLayout;

public sealed class BoxLayoutManager : IBoxLayoutManager
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

    private BoxLayoutBoxesOverlayProvider _overlayProvider = default!;

    public event Action? Started;
    public event Action<IBoxLayoutManager.BoxArgs>? Ended;
    public event Action? Cancelled;

    public bool Active { get; private set; } = false;

    public bool AttachToGrid { get; set; } = false;

    public EntityCoordinates? FirstPoint { get; private set; }

    public Color? OverlayOverrideColor { get; set; } = null;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        var overlay = BoxesOverlay.GetOverlay();
        _overlayProvider = overlay.EnsureProvider<BoxLayoutBoxesOverlayProvider>();
    }

    #region Public API
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
        SetActive(false);
        Clear();
        Cancelled?.Invoke();
    }

    public void SetOverlay(bool active)
    {
        _overlayProvider.Active = active;
    }
    #endregion

    private void RegisterInput()
    {
        CommandBinds.Builder
            .Bind(KeyFunctions220.BoxLayoutSetPoint, new PointerInputCmdHandler(
                (session, coords, target) => HandleSetPoint(coords), ignoreUp: true))
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

    private bool HandleSetPoint(EntityCoordinates coords)
    {
        if (!Active)
            return false;

        if (FirstPoint == null)
        {
            FirstPoint = coords;
            return true;
        }

        var parent = FirstPoint.Value.EntityId;
        var p1 = FirstPoint.Value.Position;

        var transform = _entity.System<TransformSystem>();
        var mapCoords = transform.ToMapCoordinates(coords);

        var map = transform.GetMapId(parent);
        if (mapCoords.MapId != map)
        {
            var error = $"Expected coordinates from map {map}, but was {mapCoords.MapId}";
            DebugTools.Assert(error);
            _console.LocalShell.WriteError(error);

            return false;
        }

        var localCoords = transform.ToCoordinates(parent, mapCoords);
        var p2 = localCoords.Position;

        var box = Box2.FromTwoPoints(p1, p2);
        if (AttachToGrid)
        {
            var gridSize = 1f;
            if (_entity.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                gridSize = mapGrid.TileSize;

            box = Box2Helper.AttachToGrid(box, gridSize);
        }

        Ended?.Invoke(new IBoxLayoutManager.BoxArgs(parent, box));
        SetActive(false);
        Clear();

        return true;
    }

    private void SetActive(bool active)
    {
        const string activeContext = "editor";

        if (Active == active)
            return;

        if (active)
        {
            _input.Contexts.SetActiveContext(activeContext);
            RegisterInput();
        }
        else
        {
            _entity.System<InputSystem>().SetEntityContextActive();
            UnregisterInput();
        }

        Active = active;
    }

    private void Clear()
    {
        FirstPoint = null;
        OverlayOverrideColor = null;
        AttachToGrid = false;
    }

    private sealed class BoxLayoutBoxesOverlayProvider() : BoxesOverlay.BoxesOverlayProvider()
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IBoxLayoutManager _boxLayoutManager = default!;
        [Dependency] private readonly IEyeManager _eye = default!;
        [Dependency] private readonly IInputManager _input = default!;

        public static readonly Color DefaultColor = Color.Green;

        public override List<BoxesOverlay.BoxOverlayData> GetBoxesDatas()
        {
            var list = new List<BoxesOverlay.BoxOverlayData>();
            if (!_boxLayoutManager.Active ||
                _boxLayoutManager.FirstPoint is not { } firstPoint)
                return list;

            var parent = firstPoint.EntityId;
            var p1 = firstPoint.Position;

            var transform = _entityManager.System<TransformSystem>();
            var mouseMapCoords = _eye.ScreenToMap(_input.MouseScreenPosition);
            if (transform.GetMapId(parent) != mouseMapCoords.MapId)
                return list;

            var p2 = transform.ToCoordinates(parent, mouseMapCoords).Position;
            var box = Box2.FromTwoPoints(p1, p2);

            if (_boxLayoutManager.AttachToGrid)
            {
                var gridSize = 1f;
                if (_entityManager.TryGetComponent<MapGridComponent>(parent, out var mapGrid))
                    gridSize = mapGrid.TileSize;

                box = Box2Helper.AttachToGrid(box, gridSize);
            }

            var color = _boxLayoutManager.OverlayOverrideColor ?? DefaultColor;
            list.Add(new BoxesOverlay.BoxOverlayData(parent, box, color.WithAlpha(0.5f)));
            return list;
        }
    }
}
