// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.LimitationRevive;

public abstract partial class SharedLimitationReviveComponent : Component
{
    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan BeforeDamageDelay = TimeSpan.FromSeconds(180);

    /// <summary>
    /// The exact time when the target will take damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan? DamageCountingTime;

    [AutoNetworkedField]
    public bool IsDamageTaken = false;
}
