// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Console;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class AddPathologyCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private PathologySystem _pathology = default!;

    public override string Command => "addpathology";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!TryResolveArguments(shell, args, EntityManager, _prototype, LocalizationManager, out var target, out var pathologyId))
            return;

        var targetName = EntityManager.ToPrettyString(target.Owner);
        if (!_pathology.TryAddPathology((target.Owner, target.Comp), pathologyId))
        {
            shell.WriteError(Loc.GetString("cmd-addpathology-failed", ("pathology", pathologyId.Id), ("entity", targetName)));
            return;
        }

        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Medium,
            $"{(shell.Player?.Name ?? "Server")} added pathology {pathologyId.Id} to {targetName}");
        shell.WriteLine(Loc.GetString("cmd-addpathology-success", ("pathology", pathologyId.Id), ("entity", targetName)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return GetPathologyCommandCompletion(args, LocalizationManager);
    }

    internal static bool TryResolveArguments(
        IConsoleShell shell,
        string[] args,
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        ILocalizationManager loc,
        out Entity<PathologyHolderComponent> target,
        out ProtoId<PathologyPrototype> pathologyId)
    {
        target = default;
        pathologyId = default;

        if (args.Length != 2)
        {
            shell.WriteError(loc.GetString("shell-wrong-arguments-number"));
            return false;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(loc.GetString("shell-entity-uid-must-be-number"));
            return false;
        }

        if (!entityManager.TryGetEntity(netEntity, out var entityUid))
        {
            shell.WriteError(loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return false;
        }

        if (!entityManager.TryGetComponent<PathologyHolderComponent>(entityUid.Value, out var holder))
        {
            shell.WriteError(loc.GetString("cmd-pathology-no-holder", ("entity", entityManager.ToPrettyString(entityUid.Value))));
            return false;
        }

        if (!prototypeManager.TryIndex<PathologyPrototype>(args[1], out var proto))
        {
            shell.WriteError(loc.GetString("cmd-pathology-proto-not-found", ("id", args[1])));
            return false;
        }

        target = (entityUid.Value, holder);
        pathologyId = new ProtoId<PathologyPrototype>(proto.ID);
        return true;
    }

    internal static CompletionResult GetPathologyCommandCompletion(string[] args, ILocalizationManager loc)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(loc.GetString("cmd-pathology-arg-entity")),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PathologyPrototype>(),
                loc.GetString("cmd-pathology-arg-pathology")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed partial class RemovePathologyCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private PathologySystem _pathology = default!;

    public override string Command => "removepathology";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!AddPathologyCommand.TryResolveArguments(shell, args, EntityManager, _prototype, LocalizationManager, out var target, out var pathologyId))
            return;

        var targetName = EntityManager.ToPrettyString(target.Owner);
        if (!_pathology.HavePathology((target.Owner, target.Comp), pathologyId))
        {
            shell.WriteError(Loc.GetString("cmd-removepathology-missing", ("pathology", pathologyId.Id), ("entity", targetName)));
            return;
        }

        if (!_pathology.TryRemovePathology((target.Owner, target.Comp), pathologyId, checkStacks: false))
        {
            shell.WriteError(Loc.GetString("cmd-removepathology-failed", ("pathology", pathologyId.Id), ("entity", targetName)));
            return;
        }

        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Medium,
            $"{(shell.Player?.Name ?? "Server")} removed pathology {pathologyId.Id} from {targetName}");
        shell.WriteLine(Loc.GetString("cmd-removepathology-success", ("pathology", pathologyId.Id), ("entity", targetName)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return AddPathologyCommand.GetPathologyCommandCompletion(args, LocalizationManager);
    }
}
