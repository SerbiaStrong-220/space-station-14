// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Language;
using Content.Shared.Administration;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Console;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddLanguageCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly LanguageManager _languageManager = default!;

    public override string Command => "addlanguage";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-entity", ("uid", args[0])));
            return;
        }

        var languageId = args[1];
        if (!_languageManager.TryGetLanguageById(languageId, out _))
        {
            shell.WriteError(Loc.GetString("cmd-language-proto-miss", ("id", languageId)));
            return;
        }

        if (!bool.TryParse(args[2], out var canSpeak))
        {
            shell.WriteError(Loc.GetString("cmd-addlanguage-can-speak-not-bool"));
            return;
        }

        var languageComp = _entManager.EnsureComponent<LanguageComponent>(uid.Value);
        var languageSystem = _entManager.System<LanguageSystem>();
        if (languageSystem.AddLanguage((uid.Value, languageComp), languageId, canSpeak))
        {
            shell.WriteLine(Loc.GetString("cmd-addlanguage-success-add"));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-addlanguage-already-have"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("cmd-language-entity-uid"),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<LanguagePrototype>(), Loc.GetString("cmd-language-id-list")),
            3 => CompletionResult.FromHintOptions(CompletionHelper.Booleans, Loc.GetString("cmd-addlanguage-can-speak")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveLanguageCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "removelanguage";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-entity", ("uid", args[0])));
            return;
        }

        var languageId = args[1];
        if (!_entManager.TryGetComponent<LanguageComponent>(uid, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        var languageSystem = _entManager.System<LanguageSystem>();
        if (languageSystem.RemoveLanguage((uid.Value, languageComp), languageId))
        {
            shell.WriteLine(Loc.GetString("cmd-removelanguage-success"));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-removelanguage-fail"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("cmd-language-entity-uid"),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<LanguagePrototype>(), Loc.GetString("cmd-language-id-list")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class ClearLanguagesCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "clearlanguages";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-entity", ("uid", args[0])));
            return;
        }

        if (!_entManager.TryGetComponent<LanguageComponent>(uid, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        var languageSystem = _entManager.System<LanguageSystem>();
        languageSystem.ClearLanguages((uid.Value, languageComp));
        shell.WriteLine(Loc.GetString("cmd-clearlanguages-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("cmd-language-entity-uid"),
            _ => CompletionResult.Empty
        };
    }
}
