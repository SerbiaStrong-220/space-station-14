using System.Linq;
using System.Text;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AnyCommand] // Corvax: Allow use to everyone
public sealed class AdminWhoCommand : IConsoleCommand
{
    public string Command => "adminwho";
    public string Description => "Returns a list of all admins on the server";
    public string Help => "Usage: adminwho";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var adminMgr = IoCManager.Resolve<IAdminManager>();
        var afk = IoCManager.Resolve<IAfkManager>();

        var seeStealth = true;

        // If null it (hopefully) means it is being called from the console.
        if (shell.Player != null)
        {
            var playerData = adminMgr.GetAdminData(shell.Player);

            seeStealth = playerData != null && playerData.CanStealth();
        }

        // SS220 fix adminwho list
        var adminList = new List<string>();

        foreach (var admin in adminMgr.ActiveAdmins)
        {
            var sb = new StringBuilder();

            var adminData = adminMgr.GetAdminData(admin)!;
            DebugTools.AssertNotNull(adminData);

            if (adminData.Stealth && !seeStealth)
                continue;

            sb.Append(admin.Name);
            if (adminData.Title is { } title)
                sb.Append($": [{title}]");

            if (adminData.Stealth)
                sb.Append(" (S)");

            if (shell.Player is { } player && adminMgr.HasAdminFlag(player, AdminFlags.Admin))
            {
                if (afk.IsAfk(admin))
                    sb.Append(" [AFK]");
            }

            adminList.Add(sb.ToString());
        }

        shell.WriteLine(string.Join(Environment.NewLine, adminList));
    }
}
