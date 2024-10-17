// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.Speech.Components;

/// <summary>
///     Applies muted to user while they wear entity as a clothing.
/// </summary>
[RegisterComponent]
public sealed partial class AddMutedClothingComponent : Component
{
    /// <summary>
    ///     Is that clothing is worn and affecting someones muted?
    /// </summary>
    public bool IsActive = false;
}
