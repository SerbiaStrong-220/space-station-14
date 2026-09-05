// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Player;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapSystem : EntitySystem
{
    private const string CultMiniMapBoundUserInterfaceName = "CultMiniMapBoundUserInterface";

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultMiniMapComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CultMiniMapComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<CultMiniMapComponent, CultMiniMapActionEvent>(OnCultMiniMapAction);
        SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
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
        _uiSystem.CloseUi(ent.Owner, CultMiniMapUIKey.Key);
        _uiSystem.SetUiState(ent.Owner, CultMiniMapUIKey.Key, null);
    }

    private void OnCultMiniMapAction(Entity<CultMiniMapComponent> ent, ref CultMiniMapActionEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        if (_uiSystem.TryToggleUi(ent.Owner, CultMiniMapUIKey.Key, actor.PlayerSession))
            args.Handled = true;
    }

    private void OnUiMessageAttempt(BoundUserInterfaceMessageAttempt args)
    {
        // The UI registration can remain on the mob after deconversion. Do not allow it
        // to be reopened without the ability, or inspected by another player's client.
        if (args.UiKey.Equals(CultMiniMapUIKey.Key)
            && (args.Actor != args.Target || !HasComp<CultMiniMapComponent>(args.Target)))
            args.Cancel();
    }
}
