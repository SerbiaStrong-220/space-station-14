// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.ItemShell;

/// <summary>
/// Persistent shell that contains its item while folded and remains inside it while unfolded.
/// </summary>
[RegisterComponent]
public sealed partial class ItemShellComponent : Component
{
    public const string ContentContainerId = "item-shell-content";

    /// <summary>
    /// Item paired with this shell, including while unfolded outside its container.
    /// </summary>
    [DataField]
    public EntityUid? LinkedItem;

    /// <summary>
    /// Sound played when the item is unfolded into a hand.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
