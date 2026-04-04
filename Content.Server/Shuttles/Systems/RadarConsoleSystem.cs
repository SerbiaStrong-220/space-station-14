using System.Numerics;
using Content.Server.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);

        // SS220 Shuttle nav info begin
        Subs.BuiEvents<RadarConsoleComponent>(RadarConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnRadarUIOpen);
            subs.Event<BoundUIClosedEvent>(OnRadarUIClose);
        });
        // SS220 Shuttle nav info end
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(uid, component);
    }

    // SS220 Shuttle nav info begin
    private void OnRadarUIOpen(Entity<RadarConsoleComponent> entity, ref BoundUIOpenedEvent args)
    {
        if ((RadarConsoleUiKey)args.UiKey != RadarConsoleUiKey.Key)
            return;

        var ev = new RadarBoundUIOpenedEvent(entity, args);
        RaiseLocalEvent(ev);
    }

    private void OnRadarUIClose(Entity<RadarConsoleComponent> entity, ref BoundUIClosedEvent args)
    {
        if ((RadarConsoleUiKey)args.UiKey != RadarConsoleUiKey.Key)
            return;

        var ev = new RadarBoundUIClosedEvent(entity, args);
        RaiseLocalEvent(ev);
    }
    // SS220 Shuttle nav info end

    protected override void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
        var xform = Transform(uid);
        var onGrid = xform.ParentUid == xform.GridUid;
        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;
        Angle? angle = onGrid ? xform.LocalRotation : null;

        if (component.FollowEntity)
        {
            coordinates = new EntityCoordinates(uid, Vector2.Zero);
            angle = Angle.Zero;
        }

        if (_uiSystem.HasUi(uid, RadarConsoleUiKey.Key))
        {
            NavInterfaceState state;
            var docks = _console.GetAllDocks();

            if (coordinates != null && angle != null)
            {
                state = _console.GetNavState(uid, docks, coordinates.Value, angle.Value);
            }
            else
            {
                state = _console.GetNavState(uid, docks);
            }

            state.RotateWithEntity = !component.FollowEntity;

            _uiSystem.SetUiState(uid, RadarConsoleUiKey.Key, new NavBoundUserInterfaceState(state));
        }
    }

    // SS220 Shuttle nav info begin
    public sealed class RadarBoundUIOpenedEvent(Entity<RadarConsoleComponent> radar, BoundUIOpenedEvent openedEvent) : EntityEventArgs
    {
        public Entity<RadarConsoleComponent> Radar = radar;
        public BoundUIOpenedEvent OpenedEvent = openedEvent;
    }

    public sealed class RadarBoundUIClosedEvent(Entity<RadarConsoleComponent> radar, BoundUIClosedEvent closedEvent) : EntityEventArgs
    {
        public Entity<RadarConsoleComponent> Radar = radar;
        public BoundUIClosedEvent ClosedEvent = closedEvent;
    }
    // SS220 Shuttle nav info end
}
