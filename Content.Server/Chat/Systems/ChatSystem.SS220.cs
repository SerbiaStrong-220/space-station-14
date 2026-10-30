// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private void SendInVoiceRangeWithLanguage(
        LanguageMessage languageMessage,
        string name,
        string verb,
        SpeechVerbPrototype speech,
        EntityUid source,
        ChatTransmitRange range)
    {
        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            var scrambledMessage = languageMessage.GetMessage(listener, true);
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;
            var wrappedMessage = WrapSpokenMessage(scrambledMessage, name, verb, speech);
            _chatManager.ChatMessageToOne(ChatChannel.Local, scrambledMessage, wrappedMessage, source, entHideChat, session.Channel);
        }

        var sourceMessage = languageMessage.GetMessage(source, false);
        var sourceWrappedMessage = WrapSpokenMessage(sourceMessage, name, verb, speech);
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Local, sourceMessage, sourceWrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));
    }

    private string WrapSpokenMessage(string message, string name, string verb, SpeechVerbPrototype speech)
    {
        var wrapId = speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message";
        return Loc.GetString(wrapId,
            ("entityName", name),
            ("verb", verb),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", message));
    }
}
