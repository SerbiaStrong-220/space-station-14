// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Investigation;
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly IInvestigationRecorder _investigation = default!;

    /// <param name="languageMessage">
    ///     Omitted on channels that carry no language, such as emotes, which record none. When given, every
    ///     language the sanitizer found is recorded alongside the selected one, since a <c>%key</c> can
    ///     switch language part-way through the sentence.
    /// </param>
    private void RecordInvestigationChat(
        EntityUid source,
        string channel,
        string message,
        string? speakerName,
        LanguageMessage? languageMessage = null)
    {
        if (!_investigation.IsRecording)
            return;

        string? selected = null;
        List<string>? spoken = null;

        if (languageMessage != null)
        {
            selected = _languageSystem.GetSelectedLanguage(source)?.ID;
            spoken = languageMessage.SpokenLanguageIds(selected);
        }

        _investigation.OnChat(source, channel, message, speakerName, selected, spoken);
    }
}
