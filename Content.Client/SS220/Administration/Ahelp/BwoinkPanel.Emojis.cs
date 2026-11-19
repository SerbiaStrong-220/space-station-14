// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.SS220.Administration.Ahelp;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Maths;

namespace Content.Client.Administration.UI.Bwoink;

public sealed partial class BwoinkPanel
{
    // Keep the existing OutputPanel whitelist and add only the catalogued image tag.
    private static readonly Type[] AhelpMessageTags =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(AhelpEmojiTag),
    ];

    private AhelpEmojiPicker? _emojiPicker;

    private void InitializeEmojis()
    {
        EmojiButton.OnPressed += _ => OpenEmojiPicker();
        OnVisibilityChanged += _ =>
        {
            if (!Visible)
                _emojiPicker?.Close();
        };
    }

    private void OpenEmojiPicker()
    {
        if (_emojiPicker == null)
        {
            _emojiPicker = new AhelpEmojiPicker();
            _emojiPicker.EmojiSelected += InsertEmoji;
            UserInterfaceManager.ModalRoot.AddChild(_emojiPicker);
        }

        var position = EmojiButton.GlobalPosition + new Vector2(0, EmojiButton.Height);
        _emojiPicker.Open(UIBox2.FromDimensions(position, Vector2.One));
    }

    private void InsertEmoji(string shortcode)
    {
        SenderLineEdit.InsertAtCursor(shortcode);
        _emojiPicker?.Close();
        SenderLineEdit.GrabKeyboardFocus();
    }

    private void DisposeEmojis()
    {
        if (_emojiPicker == null)
            return;

        _emojiPicker.EmojiSelected -= InsertEmoji;
        _emojiPicker.Close();
        _emojiPicker.Orphan();
        _emojiPicker = null;
    }
}
