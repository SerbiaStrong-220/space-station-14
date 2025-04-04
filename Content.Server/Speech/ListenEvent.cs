using Content.Shared.SS220.Language.Systems;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly bool Obfuscated; // SS220 languages
    public readonly LanguageMessage? LanguageMessage; // SS220 languages

    public ListenEvent(string message,
        EntityUid source,
        bool obfuscated = false, // SS220 languages
        LanguageMessage? languageMessage = null) // SS220 languages
    {
        Message = message;
        Source = source;
        Obfuscated = obfuscated; // SS220 languages
        LanguageMessage = languageMessage; // SS220 languages
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
