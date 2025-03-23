// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Robust.Client.Player;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Language;

public sealed class LanguageMessageTag : IMarkupTag
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public string Name => SharedLanguageSystem.LanguageMsgMarkup;

    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var text))
            return string.Empty;

        var player = _player.LocalEntity;
        if (player == null)
            return string.Empty;

        var languageSystem = _entityManager.System<LanguageSystem>();
        if (!languageSystem.ScrambledMessages.TryGetValue(text, out var languageMessage))
            return string.Empty;

        string msg;
        if (languageSystem.CanUnderstand(player.Value, languageMessage.LanguageId))
            msg = languageMessage.OriginalMessage;
        else
            msg = languageMessage.ScrambledMessage;

        return msg;
    }
}
