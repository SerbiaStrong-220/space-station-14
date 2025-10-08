// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Language.Systems;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.SS220.Language;

public sealed class LanguageMessageTag : IMarkupTagHandler
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public string Name => SharedLanguageSystem.LanguageMsgMarkup;

    private static Color DefaultTextColor = new(25, 25, 25);

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (!node.Value.TryGetString(out var key))
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        var languageSystem = _entityManager.System<LanguageSystem>();
        if (!languageSystem.TryGetPaperMessageFromKey(key, out _, out var language))
        {
            context.Color.Push(DefaultTextColor);
            return;
        }

        context.Color.Push(language.Color ?? DefaultTextColor);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Color.Pop();
    }

    /// <inheritdoc/>
    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var key))
            return string.Empty;

        var player = _player.LocalEntity;
        if (player == null)
            return string.Empty;

        var languageSystem = _entityManager.System<LanguageSystem>();
        if (!languageSystem.TryGetPaperMessageFromKey(key, out var message, out _))
            return string.Empty;

        return message;
    }
}
