// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.StationEvents.Components;

public sealed partial class SolarFlareRuleComponent
{
    /// <summary>
    /// Chance for each unprotected APC to lose power transmission when a solar flare starts.
    /// All station APCs receive the blue controller fault indicator regardless of this roll.
    /// </summary>
    [DataField]
    public float ApcFailureChance = 0.1f;
}
