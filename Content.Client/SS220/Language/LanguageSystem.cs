// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;

namespace Content.Client.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public void SelectLanguage(string languageId)
    {
        var ev = new ClientSelectLanguageEvent(languageId);
        RaiseNetworkEvent(ev);
    }
}
