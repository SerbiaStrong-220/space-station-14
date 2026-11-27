// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.SS220.Administration.Ahelp;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

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
    private readonly IGameTiming _emojiTiming = IoCManager.Resolve<IGameTiming>();
    private uint? _emojiPickerClosedFrame;

    private void InitializeEmojis()
    {
        EmojiButton.Mode = BaseButton.ActionMode.Press;
        EmojiButton.OnPressed += _ =>
        {
            // An outside click can close the popup before reaching this button.
            if (_emojiPickerClosedFrame == _emojiTiming.CurFrame)
                return;

            if (_emojiPicker is { Visible: true })
                _emojiPicker.Close();
            else
                OpenEmojiPicker();
        };
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        base.VisibilityChanged(newVisible);

        if (!newVisible)
            _emojiPicker?.Close();
    }

    protected override void ExitedTree()
    {
        DisposeEmojis();
        base.ExitedTree();
    }

    private void OpenEmojiPicker()
    {
        if (Root == null)
            return;

        if (_emojiPicker == null)
        {
            _emojiPicker = new AhelpEmojiPicker();
            _emojiPicker.EmojiSelected += InsertEmoji;
            _emojiPicker.OnPopupHide += () => _emojiPickerClosedFrame = _emojiTiming.CurFrame;
            Root.ModalRoot.AddChild(_emojiPicker);
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