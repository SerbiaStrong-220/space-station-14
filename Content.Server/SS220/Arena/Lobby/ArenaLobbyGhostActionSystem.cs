// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.SS220.Arena.Lobby;
using Robust.Shared.Player;

namespace Content.Server.SS220.Arena.Lobby;

public sealed class ArenaLobbyGhostActionSystem : EntitySystem
{
    [Dependency] private readonly ArenaLobbySystem _lobby = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArenaLobbyGhostActionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ArenaLobbyGhostActionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ArenaLobbyGhostActionComponent, OpenArenaLobbyActionEvent>(OnOpenAction);
    }

    private void OnMapInit(Entity<ArenaLobbyGhostActionComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<ArenaLobbyGhostActionComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnOpenAction(Entity<ArenaLobbyGhostActionComponent> ent, ref OpenArenaLobbyActionEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;
        args.Handled = true;
        _lobby.OpenEuiFor(actor.PlayerSession);
    }
}
