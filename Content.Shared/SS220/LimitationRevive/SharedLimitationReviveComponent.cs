// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.LimitationRevive;

[NetworkedComponent, AutoGenerateComponentState(true)]
public abstract partial class SharedLimitationReviveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DelayBeforeDamage = TimeSpan.FromSeconds(60);

    [AutoNetworkedField]
    public TimeSpan TimeToDamage = TimeSpan.Zero;

    [AutoNetworkedField]
    public bool IsDamageTaken = false;
}
