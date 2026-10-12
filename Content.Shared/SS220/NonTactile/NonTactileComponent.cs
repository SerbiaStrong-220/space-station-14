// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.NonTactile;

[RegisterComponent, NetworkedComponent]
public sealed partial class NonTactileComponent : Component
{
    public bool PopupOverridden;

    public float OriginalSuccessChance = 1f;

    public string? OriginalFailureString;

    public string? OriginalMessagePerceivedByOthers;
}

