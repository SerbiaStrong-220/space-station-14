// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Cartridges.Messenger;

/// <summary>
/// Grid of buttons with the emoji of <see cref="MessengerEmoji"/>.
/// </summary>
public sealed partial class MessengerEmojiPicker : GridContainer
{
    [Dependency] private IResourceCache _cache = default!;

    private const string EmojiFontPath = "/Fonts/NotoEmoji.ttf";
    private const int EmojiFontSize = 16;
    private const int ColumnsCount = 7;

    /// <summary>
    /// Raised with the shortcode of the picked emoji.
    /// </summary>
    public event Action<string>? OnEmojiPicked;

    /// <summary>
    /// Font that has the emoji symbols, e.g. for the button that opens the picker.
    /// </summary>
    public Font EmojiFont { get; }

    public MessengerEmojiPicker()
    {
        IoCManager.InjectDependencies(this);

        Columns = ColumnsCount;
        EmojiFont = new VectorFont(_cache.GetResource<FontResource>(EmojiFontPath), EmojiFontSize);

        foreach (var (code, symbol) in MessengerEmoji.All)
        {
            var button = new Button
            {
                Text = symbol,
                ToolTip = code,
            };
            button.Label.FontOverride = EmojiFont;
            button.OnPressed += _ => OnEmojiPicked?.Invoke(code);

            AddChild(button);
        }
    }
}
