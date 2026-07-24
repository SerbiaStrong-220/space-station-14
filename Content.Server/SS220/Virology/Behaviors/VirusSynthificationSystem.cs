// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Shared.Actions;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.Virology.Behaviors;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusSynthificationSystem : EntitySystem
{
    [Dependency] private SiliconLawSystem _siliconLaw = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusSynthificationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusSynthificationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusSynthificationComponent, GetSiliconLawsEvent>(OnGetLaws);
    }

    private void OnStartup(Entity<VirusSynthificationComponent> ent, ref ComponentStartup args)
    {
        var lawsets = _prototype.EnumeratePrototypes<SiliconLawsetPrototype>()
            .Where(lawset => !ent.Comp.ExcludedLawsets.Contains(lawset.ID))
            .ToList();
        if (lawsets.Count > 0)
        {
            // deterministic per carrier, so a re-grant (stage change) reproduce same lawset
            var rng = new Random(ent.Owner.GetHashCode());
            ent.Comp.RolledLawset = lawsets[rng.Next(lawsets.Count)].ID;
        }

        _actions.AddAction(ent.Owner, ref ent.Comp.LawsActionEntity, ent.Comp.LawsAction);
        _ui.SetUi(ent.Owner, SiliconLawsUiKey.Key, new InterfaceData("SiliconLawBoundUserInterface", requireInputValidation: false));

        if ((ent.Comp.AddBinary || ent.Comp.OnlyBinary) && TryComp<LanguageComponent>(ent, out var language))
        {
            Entity<LanguageComponent> lang = (ent.Owner, language);

            if (ent.Comp.OnlyBinary)
            {
                // remember what we take away so we can give exactly it back on cure
                ent.Comp.RemovedLanguages = [.. lang.Comp.AvailableLanguages];
                ent.Comp.OriginalSelected = lang.Comp.SelectedLanguage;
                _language.ClearLanguages(lang);
            }

            ent.Comp.AddedBinary = _language.AddLanguage(lang, ent.Comp.Binary, canSpeak: true) != null;
        }
    }

    private void OnShutdown(Entity<VirusSynthificationComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.LawsActionEntity);
        _ui.CloseUi(ent.Owner, SiliconLawsUiKey.Key);

        if (Terminating(ent.Owner) || !TryComp<LanguageComponent>(ent, out var language))
            return;

        Entity<LanguageComponent> lang = (ent.Owner, language);

        if (ent.Comp.AddedBinary)
            _language.RemoveLanguage(lang, ent.Comp.Binary);

        _language.AddLanguages(lang, ent.Comp.RemovedLanguages);

        if (ent.Comp.OriginalSelected is { } selected)
            _language.TrySelectLanguage(lang, selected);
    }

    private void OnGetLaws(Entity<VirusSynthificationComponent> ent, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || ent.Comp.RolledLawset is not { } lawset)
            return;

        args.Laws = _siliconLaw.GetLawset(lawset);
        args.Handled = true;
    }
}
