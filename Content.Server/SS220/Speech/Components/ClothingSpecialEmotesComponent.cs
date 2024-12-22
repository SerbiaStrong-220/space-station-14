// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Speech.Components;

/// <summary>
/// Allows you to use some emotions when equipped clothing with this component.
/// Must be used with <see cref="Shared.Speech.Components.VocalComponent"/> in order for emotions to be voiced.
/// </summary>
[RegisterComponent]
public sealed partial class ClothingSpecialEmotesComponent : Component
{
    /// <summary>
    /// List of emotions that can be used with these clothing
    /// </summary>
    [DataField]
    public List<ProtoId<EmotePrototype>> Emotes = new();
}
