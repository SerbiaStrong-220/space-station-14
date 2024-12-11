// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.Cultists;

[RegisterComponent, NetworkedComponent]
public sealed partial class AcsendingComponent : Component
{
    /// <summary>
    /// Time needed for ascension
    /// </summary>
    [DataField]
    public TimeSpan AcsendingInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Buffer that contains next event
    /// </summary>
    public TimeSpan AcsendingTime;

    [DataField("sprite")]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/cult_yogg_acsending.rsi"), "acsendingEffect");
}
