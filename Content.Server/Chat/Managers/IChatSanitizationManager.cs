using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Chat.Managers;

public interface IChatSanitizationManager
{
    public void Initialize();

    public bool TrySanitizeEmoteShorthands(string input,
        EntityUid speaker,
        out string sanitized,
        [NotNullWhen(true)] out string? emote,
        bool trim = true); // SS220 language

    public bool CheckNoEnglish(EntityUid speaker, string message); // SS220 no English
}
