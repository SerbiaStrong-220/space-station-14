// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Shared.Actions;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology;

public sealed partial class SynthificationSystem : EntitySystem
{
    [Dependency] private SiliconLawSystem _siliconLaw = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthificationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SynthificationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SynthificationComponent, GetSiliconLawsEvent>(OnGetLaws);
        SubscribeLocalEvent<SynthificationComponent, PathologySeverityChanged>(OnSeverityChanged);
    }

    private void OnStartup(Entity<SynthificationComponent> ent, ref ComponentStartup args)
    {
        var lawsets = _prototype.EnumeratePrototypes<SiliconLawsetPrototype>().ToList();
        if (lawsets.Count > 0)
        {
            // seed lawset roll so reactivated (or otherwise re-created)
            // Synthification reproduces same lawset
            var rng = new System.Random(ent.Owner.GetHashCode());
            ent.Comp.RolledLawset = lawsets[rng.Next(lawsets.Count)].ID;
        }

        _actions.AddAction(ent.Owner, ref ent.Comp.LawsActionEntity, ent.Comp.LawsAction);

        // all already got UserInterfaceComponent, so register laws BUI interface directly instead
        _ui.SetUi(ent.Owner, SiliconLawsUiKey.Key, new InterfaceData("SiliconLawBoundUserInterface", requireInputValidation: false));
    }

    private void OnShutdown(Entity<SynthificationComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.LawsActionEntity);
        _ui.CloseUi(ent.Owner, SiliconLawsUiKey.Key);
        if (Terminating(ent) || !ent.Comp.Snapshotted || !TryComp<LanguageComponent>(ent, out var language))
            return;

        Entity<LanguageComponent> lang = (ent.Owner, language);
        _language.ClearLanguages(lang);
        if (ent.Comp.OriginalSelected is { } selected
            && ent.Comp.OriginalLanguages.FirstOrDefault(l => l.Id == selected) is { } selectedDef)
            _language.AddLanguage(lang, selectedDef);

        _language.AddLanguages(lang, ent.Comp.OriginalLanguages);
    }

    private void OnGetLaws(Entity<SynthificationComponent> ent, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || ent.Comp.RolledLawset is not { } lawset)
            return;

        args.Laws = _siliconLaw.GetLawset(lawset);
        args.Handled = true;
    }

    private void OnSeverityChanged(Entity<SynthificationComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId != ent.Comp.Pathology || !TryComp<LanguageComponent>(ent, out var language))
            return;

        Entity<LanguageComponent> lang = (ent.Owner, language);
        switch (args.CurrentSeverity)
        {
            case 1:
                Snapshot(ent, lang);
                _language.AddLanguage(lang, ent.Comp.Binary, canSpeak: true);
                break;

            case 2:
                Snapshot(ent, lang);
                _language.ClearLanguages(lang);
                _language.AddLanguage(lang, ent.Comp.Binary, canSpeak: true);
                break;
        }
    }

    private static void Snapshot(Entity<SynthificationComponent> ent, Entity<LanguageComponent> lang)
    {
        if (ent.Comp.Snapshotted)
            return;

        ent.Comp.OriginalLanguages = new(lang.Comp.AvailableLanguages);
        ent.Comp.OriginalSelected = lang.Comp.SelectedLanguage;
        ent.Comp.Snapshotted = true;
    }
}
