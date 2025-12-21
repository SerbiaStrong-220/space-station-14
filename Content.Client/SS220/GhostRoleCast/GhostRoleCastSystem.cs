// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using Content.Shared.SS220.GhostRoleCast;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.SS220.GhostRoleCast;

public sealed class GhostRoleCastSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleCastActionEvent>(OnToggleGhostRoleCast);
        SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleRemoveActionEvent>(OnToggleGhostRoleRemove);
        SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleCastSettingsEvent>(OnToggleGhostRoleSettings);
    }

    private void OnToggleGhostRoleCast(Entity<GhostRoleCastComponent> ent, ref ToggleGhostRoleCastActionEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("action-toggle-ghostrole-cast-popup"), args.Performer);

        var flag = _playerManager.TryGetSessionByEntity(args.Performer, out var playersession);
        if (flag == false)
            return;

        var roleName = ent.Comp.GhostRoleName;
        var roleDesc = ent.Comp.GhostRoleDesc;
        var roleRule = ent.Comp.GhostRoleRule;

        if (string.IsNullOrEmpty(roleName))
            roleName = EntityManager.GetComponent<MetaDataComponent>(args.Target).EntityName;
        if (string.IsNullOrEmpty(roleDesc))
            roleDesc = EntityManager.GetComponent<MetaDataComponent>(args.Target).EntityName;
        if (string.IsNullOrEmpty(roleRule))
            roleRule = Loc.GetString("ghost-role-component-default-rules");

        var targetNetUid = GetNetEntity(args.Target);

        var makeGhostRoleCommand =
            $"makeghostrole " +
            $"\"{CommandParsing.Escape(targetNetUid.ToString())}\" " +
            $"\"{CommandParsing.Escape(roleName)}\" " +
            $"\"{CommandParsing.Escape(roleDesc)}\" " +
            $"\"{CommandParsing.Escape(roleRule)}\"";

        _consoleHost.ExecuteCommand(playersession, makeGhostRoleCommand);
        args.Handled = true;
    }

    private void OnToggleGhostRoleRemove(Entity<GhostRoleCastComponent> ent, ref ToggleGhostRoleRemoveActionEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("action-toggle-ghostrole-remove-popup"), args.Performer);

        var flag = _playerManager.TryGetSessionByEntity(args.Performer, out var playerSession);
        if (flag == false)
            return;

        var targetNetUid = GetNetEntity(args.Target);

        var removeGhostRoleCommand =
            $"rmcomp " +
            $"\"{CommandParsing.Escape(targetNetUid.ToString())}\" " +
            $"\"{CommandParsing.Escape("GhostRole")}\"";

        _consoleHost.ExecuteCommand(playerSession, removeGhostRoleCommand);

        var removeGhostTakeoverAvailableCommand =
            $"rmcomp " +
            $"\"{CommandParsing.Escape(targetNetUid.ToString())}\" " +
            $"\"{CommandParsing.Escape("GhostTakeoverAvailable")}\"";

        _consoleHost.ExecuteCommand(playerSession, removeGhostTakeoverAvailableCommand);
        args.Handled = true;
    }

    private void OnToggleGhostRoleSettings(Entity<GhostRoleCastComponent> ent, ref ToggleGhostRoleCastSettingsEvent args)
    {
        if (args.Handled)
            return;

        var uiController = _uiManager.GetUIController<GhostRoleCastUIController>();
        uiController.ToggleWindow();
    }
}
