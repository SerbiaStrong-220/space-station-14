using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface;

public static class PinUISystem
{
    private sealed class WindowData
    {
        public bool IsPinned;
        public TextureButton Button = default!;
    }

    private static readonly Dictionary<Control, WindowData> Data = new();

    public static TextureButton CreateButton(Control window, Control closeButton)
    {
        if (Data.TryGetValue(window, out var value))
            return value.Button;

        var resourceCache = IoCManager.Resolve<IResourceCache>();

        var pinnedButton = new TextureButton
        {
            TextureNormal = resourceCache.GetTexture("/Textures/SS220/Interface/pin.png"),
            ToggleMode = true,
            VerticalAlignment = Control.VAlignment.Center,
        };

        var entry = new WindowData
        {
            IsPinned = false,
            Button = pinnedButton
        };
        Data[window] = entry;

        var parent = closeButton.Parent;

        if (parent == null)
            return pinnedButton;

        if (parent is BoxContainer box && box.Children.Contains(pinnedButton))
            return pinnedButton;

        var container = new BoxContainer
        {
        };

        var closeIndex = closeButton.GetPositionInParent();

        parent.RemoveChild(closeButton);

        container.AddChild(pinnedButton);
        container.AddChild(closeButton);

        parent.AddChild(container);
        container.SetPositionInParent(closeIndex);

        return pinnedButton;
    }

    public static void SetPinned(Control window, bool pinned)
    {
        if (!Data.TryGetValue(window, out var entry))
        {
            entry = new WindowData();
            Data[window] = entry;
        }

        entry.IsPinned = pinned;
        entry.Button.Modulate = entry.IsPinned ? Color.Green : Color.White;

    }

    public static bool GetPinned(Control window)
    {
        return Data.TryGetValue(window, out var entry) && entry.IsPinned;
    }
}
