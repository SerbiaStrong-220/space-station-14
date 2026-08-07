// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.CultYogg.MiGoTeleport;
using Robust.Shared.Player;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapSystem : EntitySystem
{
    private const string CultMiniMapBoundUserInterfaceName = "CultMiniMapBoundUserInterface";//wierd

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultMiniMapComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CultMiniMapComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<CultMiniMapComponent, CultMiniMapActionEvent>(OnCultMiniMapAction);

        SubscribeLocalEvent<CultMiniMapComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnStartup(Entity<CultMiniMapComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.MiniMapActionEntity, ent.Comp.MiniMapAction);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _uiSystem.SetUi((ent, userInterfaceComp), CultMiniMapUIKey.Key, new InterfaceData(CultMiniMapBoundUserInterfaceName));
    }

    private void OnShutdown(Entity<CultMiniMapComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.MiniMapActionEntity);
    }

    private void OnCultMiniMapAction(Entity<CultMiniMapComponent> ent, ref CultMiniMapActionEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        if (_uiSystem.TryToggleUi(ent.Owner, CultMiniMapUIKey.Key, actor.PlayerSession))
            args.Handled = true;
    }

    private void OnUIOpened(Entity<CultMiniMapComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent, ent.Comp);
    }

    private void UpdateUserInterface(EntityUid ent, CultMiniMapComponent? component = null)//not sure, maybe should be Entity<CultMiniMapComponent> ent
    {
        if (!Resolve(ent, ref component))
            return;

        if (!_uiSystem.IsUiOpen(ent, CultMiniMapUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(ent);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        //var allSensors = component.ConnectedSensors.Values.ToList();
        //_uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }
}
