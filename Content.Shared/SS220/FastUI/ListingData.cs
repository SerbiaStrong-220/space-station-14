using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.FastUI;

[Serializable, NetSerializable]
public sealed class ListingData
{
    public string ID;
    public string Title;
    public string Description;
    public string SubDescription;
    public string ButtonText;
    public ButtonState ButtonState;
    public SpriteSpecifier? Icon;

    public ListingData(string id, string title, string description, string subDescription, string buttonText, ButtonState buttonState, SpriteSpecifier? icon = null)
    {
        ID = id;
        Title = title;
        Description = description;
        SubDescription = subDescription;
        ButtonText = buttonText;
        ButtonState = buttonState;
        Icon = icon;
    }
}

[Serializable, NetSerializable]
public enum ButtonState : byte
{
    Enabled,
    Disabled,
    Hided
}

public static class ListingDataUtils
{
    public static ButtonState ToButtonState(this Enum source)
    {
        if (Enum.TryParse<ButtonState>(source.ToString(), true, out var result))
        {
            return result;
        }

        return ButtonState.Enabled;
    }
}
