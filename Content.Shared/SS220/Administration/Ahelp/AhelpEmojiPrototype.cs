// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Administration.Ahelp;

/// <summary>An Ahelp image and its original Discord custom emoji.</summary>
[Prototype]
public sealed partial class AhelpEmojiPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public string DiscordId = default!;

    [DataField(required: true)]
    public ResPath Texture;

    public string Shortcode => $":{ID}:";
    public string DiscordMarkup => $"<:{Name}:{DiscordId}>";
}
