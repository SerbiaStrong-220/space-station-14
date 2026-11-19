// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.SS220.Administration.Ahelp;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Administration.Ahelp;

public sealed partial class AhelpEmojiTag : IMarkupTagHandler
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IResourceCache _resources = default!;

    public string Name => AhelpEmojiText.MarkupTag;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!node.Value.TryGetString(out var id) ||
            !_prototypes.TryIndex<AhelpEmojiPrototype>(id, out var emoji))
            return false;

        control = new TextureRect
        {
            Texture = _resources.GetResource<TextureResource>(emoji.Texture).Texture,
            SetSize = new Vector2(24, 24),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            ToolTip = emoji.Shortcode,
        };
        return true;
    }
}
