// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.SS220.CruiseControl;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.SS220.WeaponControl;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Server.Shuttles.Systems;

namespace Content.Server.SS220.WeaponControl;

public sealed partial class WeaponControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ShuttleConsoleSystem _shuttleConsole = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponControlConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<WeaponControlConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
        SubscribeLocalEvent<WeaponControlConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
        SubscribeLocalEvent<WeaponControlConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt, after: [typeof(ActivatableUIRequiresAccessSystem)]);

        Subs.BuiEvents<WeaponControlConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnConsoleUIClose);
        });
    }

    private void OnConsoleShutdown(Entity<WeaponControlConsoleComponent> ent, ref ComponentShutdown args)
    {
        //ClearPilots(component);
    }

    private void OnConsolePowerChange(Entity<WeaponControlConsoleComponent> ent, ref PowerChangedEvent args)
    {
        DockingInterfaceState? dockState = null;
        UpdateState(ent, ref dockState);
    }
    private void OnConsoleAnchorChange(Entity<WeaponControlConsoleComponent> ent, ref AnchorStateChangedEvent args)
    {
        DockingInterfaceState? dockState = null;
        UpdateState(ent, ref dockState);
    }

    private void OnConsoleUIOpenAttempt(Entity<WeaponControlConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        //if (!TryPilot(args.User, uid))
        //    args.Cancel();
    }
    private void OnConsoleUIClose(Entity<WeaponControlConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if ((WeaponControlConsoleUiKey)args.UiKey != WeaponControlConsoleUiKey.Key)
            return;

        //RemovePilot(args.Actor);
    }

    private void UpdateState(EntityUid consoleUid, ref DockingInterfaceState? dockState)
    {
        EntityUid? entity = consoleUid;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = entity,
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        TryComp(entity, out TransformComponent? consoleXform);
        var shuttleGridUid = consoleXform?.GridUid;

        NavInterfaceState navState;
        dockState ??= _shuttleConsole.GetDockState();

        if (shuttleGridUid != null && entity != null)
        {
            //navState = _shuttleConsole.GetNavState(entity.Value, dockState.Docks);
            navState = _shuttleConsole.GetNavState(consoleUid, dockState.Docks);
        }
        else
        {
            navState = new NavInterfaceState(0f, null, null, []);
        }

        if (_ui.HasUi(consoleUid, WeaponControlConsoleUiKey.Key))
        {
            _ui.SetUiState(consoleUid, WeaponControlConsoleUiKey.Key, new WeaponControlBoundUserInterfaceState(navState));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
}
