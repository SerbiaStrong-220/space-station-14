// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.Undereducated;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SS220.Undereducated;

public sealed class UndereducatedClientSystem : EntitySystem
{
    private UndereducatedWindow? _window;
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UndereducatedComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<UndereducatedComponent, PlayerDetachedEvent>(OnPlayerDeattached);
    }

    private void OnPlayerAttached(Entity<UndereducatedComponent> ent, ref PlayerAttachedEvent args)
    {
        if (args.Entity != _playerManager.LocalSession?.AttachedEntity)
            return;

        if (!ent.Comp.Tuned && TryComp<LanguageComponent>(ent, out var langComp)
            && langComp.AvailableLanguages.Count > 0)
        {
            if (!TryGetSpokenLanguageList(langComp, out var langList))
                return;

            _window?.Close();
            _window = new UndereducatedWindow(ent.Comp, langList);
            _window.OnClose += () =>
            {
                if (ent != _playerManager.LocalSession?.AttachedEntity)
                    return;

                if (!TryComp<UndereducatedComponent>(ent, out var comp) || comp.Tuned)
                    _window = null;
                else
                {
                    var ev = new UndereducatedConfigRequestEvent(GetNetEntity(ent), _window.SelectedLanguage, _window.SelectedChance);
                    RaiseNetworkEvent(ev);
                    _window = null;
                }
            };
            _window.OpenCentered();
        }
    }

    private bool TryGetSpokenLanguageList(LanguageComponent langComp, out List<string> langList)
    {
        langList = [];
        var i = 0;
        while (i <= langComp.AvailableLanguages.Count - 1)
        {
            if (langComp.AvailableLanguages[i].Id != _languageSystem.GalacticLanguage
                && langComp.AvailableLanguages[i].Id != _languageSystem.UniversalLanguage
                && langComp.AvailableLanguages[i].CanSpeak)
                langList.Add(langComp.AvailableLanguages[i].Id);
            i++;
        }
        if (langList.Count <= 0)
            return false;

        return true;
    }

    private void OnPlayerDeattached(Entity<UndereducatedComponent> ent, ref PlayerDetachedEvent args)
    {
        if (args.Entity == _playerManager.LocalSession?.AttachedEntity)
            _window?.Close();
    }
}
