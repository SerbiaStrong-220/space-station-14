// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent, NetworkedComponent]
public sealed partial class MalfAiHackableComponent : Component
{
    /// <summary>
    /// Cooldown paid by the hacking action after successfully capturing this device.
    /// </summary>
    [DataField]
    public TimeSpan HackCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// CPU income relative to a normal APC, using the same payout interval.
    /// </summary>
    [DataField]
    public int RewardMultiplier = 1;
}
