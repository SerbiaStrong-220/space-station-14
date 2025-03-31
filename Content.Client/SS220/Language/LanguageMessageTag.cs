// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.SS220.Language;

public sealed class LanguageMessageTag : IMarkupTag
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public string Name => SharedLanguageSystem.LanguageMsgMarkup;

    private static Color DefaultTextColor = new(25, 25, 25);

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!node.Value.TryGetString(out var key))
            return false;

        var player = _player.LocalEntity;
        if (player == null)
            return false;

        var languageSystem = _entityManager.System<LanguageSystem>();
        if (!languageSystem.TryGetPaperMessageFromKey(key, out var message, out var language))
            return false;

        var label = new Label
        {
            Text = message,
            FontColorOverride = language.Color ?? DefaultTextColor,
        };
        control = label;
        return true;
    }
}
