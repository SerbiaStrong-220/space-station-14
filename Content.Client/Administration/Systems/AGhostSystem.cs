using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Client.Administration.Components;
using Robust.Client.Console;
using Robust.Shared.Utility;
using Robust.Client.Player;

namespace Content.Client.Administration.Systems
{
    public sealed class AGhostSystem : EntitySystem
    {

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AGhostComponent, ComponentInit>(OnAGhostInit);

            SubscribeLocalEvent<AGhostComponent, ToggleGhostRoleCastActionEvent>(OnToggleGhostRoleCast);
            SubscribeLocalEvent<AGhostComponent, ToggleGhostRoleRemoveActionEvent>(OnToggleGhostRoleRemove);

        }
        private void OnAGhostInit(EntityUid uid, AGhostComponent component, ComponentInit args)
        {
            _actions.AddAction(uid, component.ToggleGhostRoleCastAction, null);
            _actions.AddAction(uid, component.ToggleGhostRoleRemoveAction, null);
        }

        private void OnToggleGhostRoleCast(EntityUid uid, AGhostComponent component, ToggleGhostRoleCastActionEvent args)
        {
            if (args.Handled)
                return;
            
            _popup.PopupEntity(Loc.GetString("aghost-toggle-ghostrole-cast-popup"), args.Performer);

            var player = _playerManager.LocalPlayer;
            if (player == null)
            {
                return;
            }

            var makeGhostRoleCommand =
                $"makeghostrole " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape("EventRole")}\" " +
                $"\"{CommandParsing.Escape(" ")}\" " +
                $"\"{CommandParsing.Escape("Enjoy Yourself")}\"";

            _consoleHost.ExecuteCommand(player.Session, makeGhostRoleCommand);

            //if (makesentient)
            //{
            //    var makesentientcommand = $"makesentient \"{commandparsing.escape(uid.tostring())}\"";
            //    _consolehost.executecommand(player.session, makesentientcommand);
            //}

            args.Handled = true;
        }

        private void OnToggleGhostRoleRemove(EntityUid uid, AGhostComponent component, ToggleGhostRoleRemoveActionEvent args)
        {
            if (args.Handled)
                return;

            _popup.PopupEntity(Loc.GetString("aghost-toggle-ghostrole-remove-popup"), args.Performer);

            var player = _playerManager.LocalPlayer;
            if (player == null)
            {
                return;
            }
            
            var removeGhostRoleCommand =
                $"rmcomp " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape("GhostRole")}\"";

            _consoleHost.ExecuteCommand(player.Session, removeGhostRoleCommand);

            var removeGhostTakeoverAvailableCommand =
                $"rmcomp " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape("GhostTakeoverAvailable")}\"";

            _consoleHost.ExecuteCommand(player.Session, removeGhostTakeoverAvailableCommand);

            args.Handled = true;
        }
    }
}
