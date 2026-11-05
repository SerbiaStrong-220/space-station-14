// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.Cultists;

[RegisterComponent, NetworkedComponent]
public sealed partial class AscendingComponent : Component
{
    /// <summary>
    /// Time needed for ascension
    /// </summary>
    [DataField]
    public TimeSpan AscendingInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Buffer that contains next event
    /// </summary>
    public TimeSpan AscendingTime;

    [DataField]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/CultYogg/ascending.rsi"), "ascendingEffect");
}
