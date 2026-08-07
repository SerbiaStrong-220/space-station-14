// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <summary>
    ///     Language aware version of <see cref="SendInVoiceRange"/>.
    ///     Every listener receives its own variant of the message.
    /// </summary>
    private void SendInVoiceRangeWithLanguage(
        LanguageMessage languageMessage,
        string name,
        string verb,
        SpeechVerbPrototype speech,
        EntityUid source,
        ChatTransmitRange range)
    {
        var wrapId = speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message";

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            var scrambledMessage = languageMessage.GetMessage(listener, true);
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;
            _chatManager.ChatMessageToOne(ChatChannel.Local, scrambledMessage, Wrap(scrambledMessage), source, entHideChat, session.Channel);
        }

        var sourceMessage = languageMessage.GetMessage(source, false);
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Local, sourceMessage, Wrap(sourceMessage), GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        string Wrap(string message) => Loc.GetString(wrapId,
            ("entityName", name),
            ("verb", verb),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", message));
    }
}
