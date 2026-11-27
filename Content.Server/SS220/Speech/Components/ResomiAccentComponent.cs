// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Speech.Components;

/// <summary>
/// The resomi accent component.
/// Doubles all "z"/"з" letters and inserts "щ" after "ц"/"ч" (Russian) and "sh" after "c"/"ch" (English)
/// </summary>
[RegisterComponent]
public sealed partial class ResomiAccentComponent : Component
{
}