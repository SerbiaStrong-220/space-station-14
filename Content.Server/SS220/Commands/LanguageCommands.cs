// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Language;
using Content.Shared.Administration;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Console;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddLanguageCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly LanguageManager _languageManager = default!;

    public string Command => "addlanguage";
    public string Description => Loc.GetString("cmd-language-add-desc");
    public string Help => "addlanguage <entityId> <languageId> <canSpeak>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError("addlanguage <entityId> <languageId> <canSpeak>");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityId))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-id"));
            return;
        }

        if (!bool.TryParse(args[2], out var canSpeak))
        {
            return;
        }

        var languageId = args[1];

        if (!_languageManager.TryGetLanguageById(languageId, out _))
        {
            shell.WriteError(Loc.GetString("cmd-language-proto-miss"));
            return;
        }

        if (!_entities.TryGetComponent<LanguageComponent>(entityId, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        var languageSystem = _entities.System<LanguageSystem>();
        if (languageSystem.AddLanguage((entityId, languageComp), languageId, canSpeak))
        {
            shell.WriteLine(Loc.GetString("cmd-language-success-add"));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-language-already-have"));
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveLanguageCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "removelanguage";
    public string Description => Loc.GetString("cmd-language-remove-desc");
    public string Help => "removelanguage <entityId> <languageId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("removelanguage <entityId> <languageId>");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityId))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-id"));
            return;
        }

        var languageId = args[1];

        if (!_entities.TryGetComponent<LanguageComponent>(entityId, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        var languageSystem = _entities.System<LanguageSystem>();
        if (languageSystem.RemoveLanguage((entityId, languageComp), languageId))
        {
            shell.WriteLine(Loc.GetString("cmd-language-succes-remove"));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-language-fail-remove"));
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class ClearLanguagesCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "clearlanguages";
    public string Description => Loc.GetString("cmd-language-clear-desc");
    public string Help => "clearlanguages <entityId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("clearlanguages <entityId>");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityId))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-id"));
            return;
        }

        if (!_entities.TryGetComponent<LanguageComponent>(entityId, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        var languageSystem = _entities.System<LanguageSystem>();
        languageSystem.ClearLanguages((entityId, languageComp));
        shell.WriteLine(Loc.GetString("cmd-language-clear"));
    }
}

