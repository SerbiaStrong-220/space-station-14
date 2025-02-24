using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.Language;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddLanguageCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public string Command => "addlanguage";
    public string Description => Loc.GetString("cmd-language-add-desc");
    public string Help => "addlanguage <entityId> <languageId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("addlanguage <entityId> <languageId>");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityId))
        {
            shell.WriteError(Loc.GetString("cmd-language-invalid-id"));
            return;
        }

        var languageId = args[1];

        if (!_proto.TryIndex<LanguagesPrototype>(languageId, out var languageProto))
        {
            shell.WriteError(Loc.GetString("cmd-language-proto-miss"));
            return;
        }

        if (!_entities.TryGetComponent<LanguageComponent>(entityId, out var languageComp))
        {
            shell.WriteError(Loc.GetString("cmd-language-comp-miss"));
            return;
        }

        if (!languageComp.LearnedLanguages.Contains(languageId))
        {
            languageComp.LearnedLanguages.Add(languageId);
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

        if (languageComp.LearnedLanguages.Remove(languageId))
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

        languageComp.LearnedLanguages.Clear();
        shell.WriteLine(Loc.GetString("cmd-language-clear"));
    }
}

