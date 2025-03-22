// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;

namespace Content.Client.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateScrambledMessagesEvent>(OnUpdateScrambledMessages);
        SubscribeNetworkEvent<UpdateLanguageSeedEvent>(OnUpdateLanguageSeed);
    }

    private void OnUpdateScrambledMessages(UpdateScrambledMessagesEvent ev)
    {
        ScrambledMessages = ev.ScrambledMessages;
    }

    private void OnUpdateLanguageSeed(UpdateLanguageSeedEvent ev)
    {
        Seed = ev.Seed;
    }

    public void SelectLanguage(string languageId)
    {
        var ev = new ClientSelectLanguageEvent(languageId);
        RaiseNetworkEvent(ev);
    }

    protected override void AddScrambledMessage(LanguageMessage newMsg)
    {
        base.AddScrambledMessage(newMsg);
        var ev = new ClientAddScrambledMessageEvent(newMsg.ScrambledMessage, newMsg);
        RaiseNetworkEvent(ev);
    }
}
