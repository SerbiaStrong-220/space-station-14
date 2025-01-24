// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Preferences;

namespace Content.Server.SS220.RenameStart;

/// <summary>
/// This is used for change the entity name once the player starts controlling
/// </summary>
[RegisterComponent]
public sealed partial class RenameStartComponent : Component
{
    [DataField]
    public int MinChar = 2;

    [DataField]
    public int MaxChar = HumanoidCharacterProfile.MaxNameLength;
}
